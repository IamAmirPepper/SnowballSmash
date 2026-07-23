using AudioSystem;
using SnowballSmash.Events;
using UnityEngine;

namespace SnowballSmash.Gameplay
{
    public class PlayerCollision : MonoBehaviour
    {
        [SerializeField] private AudioEvent nearMissRightEvent;
        [SerializeField] private AudioEvent nearMissLeftEvent;
        [SerializeField] private AudioEvent impactEvent;
        

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
                        impactEvent.Post(gameObject);
                    }
                    else if(collision.TryGetComponent<Obstacle>(out Obstacle obstacle))
                    {
                        //Debug.Log("hit obstacle");
                        collisionEvents.RaiseOnObstacleHit();
                        impactEvent.Post(gameObject);
                    }
                }


                if (distance > distanceThreshold)
                {
                    //Debug.Log("NEAR MISS!");
                    collisionEvents.RaiseOnNearMiss();

                    // Calculate the difference on the X axis
                    float xDifference = trueObj.transform.position.x - GetCenterPoint().x;

                    if (xDifference > 0)
                    {
                        Debug.Log("NEAR MISS ON THE RIGHT!");
                        nearMissRightEvent.Post(gameObject);
                        // collisionEvents.RaiseOnNearMissRight(); 
                    }
                    else if (xDifference < 0)
                    {
                        Debug.Log("NEAR MISS ON THE LEFT!");
                        nearMissLeftEvent.Post(gameObject);
                        // collisionEvents.RaiseOnNearMissLeft();
                    }
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
