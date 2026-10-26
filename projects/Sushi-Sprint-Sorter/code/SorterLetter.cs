using UnityEngine;

namespace SorterGame
{
    public class SorterLetter : MonoBehaviour
    {
        private SorterGameManager manager;
        private float speed;
        private float laneEaseVelocity;
        private int previousLane;

        public MailType TargetType { get; private set; }
        public int CurrentLane { get; private set; }
        public bool IsDragging { get; private set; }

        public void Initialize(SorterGameManager owner, MailType targetType, int startLane, float moveSpeed)
        {
            manager = owner;
            TargetType = targetType;
            CurrentLane = Mathf.Clamp(startLane, 0, 2);
            speed = moveSpeed;
            ApplyDepthScale(1f);
            EnsureCollider();
            manager.RegisterLetter(this);
        }

        private void Update()
        {
            if (manager == null || manager.State != SorterGameState.Playing || manager.IsPaused)
            {
                return;
            }

            Vector3 position = transform.position;
            if (!IsDragging)
            {
                position.y += speed * manager.ActiveLetterSpeedMultiplier * Time.deltaTime;
                position.x = Mathf.SmoothDamp(position.x, manager.LaneX(CurrentLane, position.y), ref laneEaseVelocity, 0.09f);
                position.z = 0f;
                transform.position = position;
                ApplyDepthScale(1f);
            }

            if (transform.position.y >= manager.JudgeY)
            {
                manager.ResolveLetter(this);
            }
        }

        public void SetLane(int lane)
        {
            CurrentLane = Mathf.Clamp(lane, 0, 2);
        }

        public void BeginDrag()
        {
            if (IsDragging)
            {
                return;
            }

            previousLane = CurrentLane;
            IsDragging = true;
            laneEaseVelocity = 0f;
            ApplyDepthScale(1.22f);
            Vector3 position = transform.position;
            position.z = -0.58f;
            transform.position = position;
        }

        public void DragToWorldX(float worldX)
        {
            if (!IsDragging)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x = Mathf.Clamp(worldX, manager.LaneX(0, position.y), manager.LaneX(2, position.y));
            position.z = -0.58f;
            transform.position = position;
            ApplyDepthScale(1.22f);
        }

        public void EndDragToNearestLane()
        {
            if (!IsDragging)
            {
                return;
            }

            CurrentLane = manager.NearestLaneForWorldX(transform.position.x, transform.position.y);
            IsDragging = false;
            Vector3 position = transform.position;
            position.x = manager.LaneX(CurrentLane, position.y);
            position.z = 0f;
            transform.position = position;
            ApplyDepthScale(1f);
        }

        public void CancelDrag()
        {
            if (!IsDragging)
            {
                return;
            }

            CurrentLane = previousLane;
            IsDragging = false;
            Vector3 position = transform.position;
            position.x = manager.LaneX(CurrentLane, position.y);
            position.z = 0f;
            transform.position = position;
            ApplyDepthScale(1f);
        }

        private void ApplyDepthScale(float liftMultiplier)
        {
            if (manager == null)
            {
                return;
            }

            transform.localScale = manager.LetterScaleForY(transform.position.y) * liftMultiplier;
        }

        private void EnsureCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;
            box.size = new Vector3(1.45f, 1.65f, 0.55f);
            box.center = Vector3.zero;
        }
    }
}
