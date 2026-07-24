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

        [Tooltip("Distance from the snowball's centre to an obstacle's collider surface that counts as a hit. Anything further is a near miss.")]
        [SerializeField] private float hitRadius = 0.5f;

        [SerializeField] private GameLifeCycleEvents lifeCycleEvents;
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
           triggerhandler(collision);
        }

        private void triggerhandler(Collider2D collision)
        {
            if (lifeCycleEvents.hasGameStarted == false) return;

            if (collision.TryGetComponent<CollidableBase>(out CollidableBase trueObj))
            {
                //var distance = Vector2.Distance(GetCenterPoint(), trueObj.transform.position);
                var distance = Mathf.Abs(trueObj.transform.position.x - GetCenterPoint().x);

                if (distance < distanceThreshold)
                {
                    if (collision.TryGetComponent<Target>(out Target target))
                    {
                        //Debug.Log("hit target");
                        collisionEvents.RaiseOnCollectHit();
                        impactEvent.Post(gameObject);
                    }
                    else if (collision.TryGetComponent<Obstacle>(out Obstacle obstacle))
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

      /*  private void HandleNearMiss(CollidableBase trueObj, Vector2 myCenter)
        {
            collisionEvents.RaiseOnNearMiss();

            float xDiff = trueObj.transform.position.x - myCenter.x;
            if(xDiff > 0) nearMissRightEvent.Post(gameObject);
            else nearMissLeftEvent.Post(gameObject);
        }
        private void alternateTriggering(Collider2D collision)
        {
            if (lifeCycleEvents.hasGameStarted == false) return;
            if (!collision.TryGetComponent(out CollidableBase trueObj)) return;


            if (collision.TryGetComponent<Target>(out Target target))
            {
                //Debug.Log("hit target");
                collisionEvents.RaiseOnCollectHit();
                impactEvent.Post(gameObject);
            }
            else if (collision.TryGetComponent<Obstacle>(out Obstacle obstacle))
            {
                //Debug.Log("hit obstacle");
                collisionEvents.RaiseOnObstacleHit();
                impactEvent.Post(gameObject);
            }


            Vector2 myCenter = GetCenterPoint();

            float gap = Vector2.Distance(myCenter, collision.ClosestPoint(myCenter));

            if (gap >= hitRadius)
            {
                HandleNearMiss(trueObj, myCenter);
            }
            //triggerhandler(collision);
        }*/


        private Vector2 GetCenterPoint()
        {
            if (_myCollider != null) return _myCollider.bounds.center;
            if (_myRenderer != null) return _myRenderer.bounds.center;

            return _myCenterPoint = transform.position;
        }

    }
}
