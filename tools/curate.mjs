import fs from "node:fs/promises";
import path from "node:path";
import { createHash } from "node:crypto";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const plan = JSON.parse(await fs.readFile(path.join(root, "selection-plan.json"), "utf8"));
const args = process.argv.slice(2);
const mode = args[0];
if (!["export", "verify"].includes(mode) || args.length > 2 || (mode === "export" && !args[1])) {
  throw new Error("Usage: node tools/curate.mjs export <source-root> | verify [source-root]");
}
const sourceRoot = args[1] ? path.resolve(args[1]) : null;
const digest = (bytes) => createHash("sha256").update(bytes).digest("hex");
const projectFor = (slug) => {
  const project = plan.projects.find((item) => item.slug === slug);
  if (!project) throw new Error(`Unknown project: ${slug}`);
  return project;
};
function inside(base, relative) {
  const result = path.resolve(base, relative);
  if (!result.startsWith(base + path.sep)) throw new Error(`Path outside root: ${relative}`);
  return result;
}

function materialize(sample, sourceBytes) {
  if (sample.kind === "original-file") return sourceBytes;
  const lines = sourceBytes.toString("utf8").split(/(?<=\n)/);
  const sections = sample.ranges.map(([start, end]) => {
    if (start < 1 || end < start || end > lines.length) {
      throw new Error(`Invalid range: ${sample.source}:${start}-${end}`);
    }
    return `// Source lines ${start}-${end}\n${lines.slice(start - 1, end).join("")}`;
  });
  const notice = sample.kind === "context-excerpt"
    ? "// READING EXCERPT: members of the original class; not a standalone C# compilation unit.\n"
    : "// CLASS EXTRACTION: original class bodies; only imports, namespace and source comments added.\n";
  return Buffer.from([
    notice,
    `// Source: ${sample.source}\n`,
    `// Dependencies: ${sample.dependencies}\n\n`,
    sample.wrapperPrefix || "",
    sections.join("\n"),
    sample.wrapperSuffix || "",
  ].join(""));
}

const manifestPath = path.join(root, "source-manifest.json");
if (mode === "export") {
  try {
    await fs.access(manifestPath);
    throw new Error("An export already exists. Verify it or create a separate destination.");
  } catch (error) {
    if (error.code !== "ENOENT") throw error;
  }
  const entries = [];
  const sourceSnapshots = new Map();
  for (const sample of plan.samples) {
    const project = projectFor(sample.project);
    const sourcePath = inside(inside(sourceRoot, project.sourceDirectory), sample.source);
    const bytes = await fs.readFile(sourcePath);
    const sourceHash = digest(bytes);
    if (sourceSnapshots.has(sourcePath) && sourceSnapshots.get(sourcePath) !== sourceHash) {
      throw new Error(`Source changed during export: ${sample.source}`);
    }
    sourceSnapshots.set(sourcePath, sourceHash);
    const outputBytes = materialize(sample, bytes);
    const outputPath = inside(root, sample.output);
    await fs.mkdir(path.dirname(outputPath), { recursive: true });
    await fs.writeFile(outputPath, outputBytes, { flag: "wx" });
    entries.push({
      project: sample.project,
      source: sample.source,
      output: sample.output,
      kind: sample.kind,
      ranges: sample.ranges || null,
      sourceSha256: sourceHash,
      outputSha256: digest(outputBytes),
      bytes: outputBytes.length,
      lines: outputBytes.toString("utf8").split("\n").length - 1,
    });
  }
  for (const [file, hash] of sourceSnapshots) {
    if (digest(await fs.readFile(file)) !== hash) throw new Error("Source changed during export.");
  }
  await fs.writeFile(manifestPath, JSON.stringify({
    formatVersion: 1,
    preparedOn: plan.preparedOn,
    selectionPlanSha256: digest(await fs.readFile(path.join(root, "selection-plan.json"))),
    samples: entries,
  }, null, 2) + "\n", { flag: "wx" });
  console.log(`Exported ${entries.length} samples from ${sourceSnapshots.size} original files.`);
}

const manifest = JSON.parse(await fs.readFile(manifestPath, "utf8"));
if (digest(await fs.readFile(path.join(root, "selection-plan.json"))) !== manifest.selectionPlanSha256) {
  throw new Error("Selection plan differs from the exported snapshot.");
}
if (manifest.samples.length !== plan.samples.length) throw new Error("Sample count mismatch.");
let sourceCount = 0;
for (const sample of plan.samples) {
  const entry = manifest.samples.find((item) => item.output === sample.output);
  if (!entry) throw new Error(`Missing manifest entry: ${sample.output}`);
  const bytes = await fs.readFile(inside(root, sample.output));
  if (digest(bytes) !== entry.outputSha256 || bytes.length !== entry.bytes) {
    throw new Error(`Output differs from snapshot: ${sample.output}`);
  }
  if (sourceRoot) {
    const sourcePath = inside(inside(sourceRoot, projectFor(sample.project).sourceDirectory), sample.source);
    const source = await fs.readFile(sourcePath);
    if (digest(source) !== entry.sourceSha256) throw new Error(`Source snapshot changed: ${sample.source}`);
    if (!bytes.equals(materialize(sample, source))) throw new Error(`Extraction differs: ${sample.output}`);
    sourceCount++;
  }
}

async function markdownFiles(directory) {
  const files = [];
  for (const item of await fs.readdir(directory, { withFileTypes: true })) {
    const file = path.join(directory, item.name);
    if (item.isDirectory() && !item.name.startsWith(".")) files.push(...await markdownFiles(file));
    else if (item.isFile() && item.name.endsWith(".md")) files.push(file);
  }
  return files;
}
let links = 0;
for (const file of await markdownFiles(root)) {
  const content = await fs.readFile(file, "utf8");
  for (const match of content.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
    const target = match[1];
    if (/^(https?:|mailto:|#)/.test(target)) continue;
    const relative = decodeURIComponent(target.split("#")[0]);
    await fs.access(path.resolve(path.dirname(file), relative));
    links++;
  }
}
console.log(`Verified ${manifest.samples.length} sample hashes and ${links} local Markdown links.`);
if (sourceRoot) console.log(`Verified ${sourceCount} exact extractions against the original sources.`);
console.log("Unity compilation, PlayMode tests, and device behavior were not executed by this tool.");
