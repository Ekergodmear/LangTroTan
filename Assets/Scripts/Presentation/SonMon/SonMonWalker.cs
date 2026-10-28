using UnityEngine;

namespace LangTroTan.Presentation
{
    // Scenic route for prototype characters (not a server entity): walks between waypoints and pauses.
    public sealed class SonMonWalker : MonoBehaviour
    {
        public Vector3[] waypoints;
        public float speed = .7f, pause = 2.5f;
        int target;
        float wait;
        SonMonPuppet puppet;

        void Awake() => puppet = GetComponent<SonMonPuppet>();

        void Update()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            if (wait > 0)
            {
                wait -= Time.deltaTime;
                if (puppet != null) puppet.speed = 0;
                return;
            }
            var goal = waypoints[target];
            var delta = goal - transform.position;
            delta.y = 0;
            if (delta.magnitude < .05f)
            {
                target = (target + 1) % waypoints.Length;
                wait = pause;
                return;
            }
            var step = Vector3.ClampMagnitude(delta, speed * Time.deltaTime);
            transform.position += step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-delta.normalized), Time.deltaTime * 5);
            if (puppet != null) puppet.speed = speed;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 6.5f)) * .012f;
            transform.position = new Vector3(transform.position.x, bob, transform.position.z);
        }
    }
}
