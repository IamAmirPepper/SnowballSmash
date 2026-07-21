using SnowballSmash.Events;
using UnityEngine;

namespace SnowballSmash.Gameplay
{
    public class PlayerCollision : MonoBehaviour
    {

        [SerializeField] private float distanceThreshold = 2f;
        [SerializeField] private SnowballCollisionEvents collisionEvents;

        private Collider2D _myCollider;
        private SpriteRenderer _myRenderer;
        private Vector2 _myCenterPoint;

        private void Start()
        {
            _myCollider = GetComponent<Collider2D>();
            _myRenderer = GetComponent<SpriteRenderer>();
            _myCenterPoint = _myCollider.bounds.center;
        }


        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent<CollidableBase>(out CollidableBase trueObj))
            {
                var distance = Vector2.Distance(GetCenterPoint(), trueObj.transform.position);


                if(distance < distanceThreshold)
                {
                    if(collision.TryGetComponent<Target>(out Target target))
                    {
                        //Debug.Log("hit target");
                        collisionEvents.RaiseOnCollectHit();
                    }
                    else if(collision.TryGetComponent<Obstacle>(out Obstacle obstacle))
                    {
                        //Debug.Log("hit obstacle");
                        collisionEvents.RaiseOnObstacleHit();
                    }
                }


                if (distance > distanceThreshold)
                {
                    //Debug.Log("NEAR MISS!");
                    collisionEvents.RaiseOnNearMiss();
                }
            }
        }

        private Vector2 GetCenterPoint()
        {
            if (_myCollider != null) return _myCollider.bounds.center;
            if (_myRenderer != null) return _myRenderer.bounds.center;

            return _myCenterPoint = transform.position;
        }

    }
}
