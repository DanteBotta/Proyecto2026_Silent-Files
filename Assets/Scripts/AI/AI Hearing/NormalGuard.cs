using GamePlay;
using UnityEngine;
using UnityEngine.AI;

public class NormalGuard : MonoBehaviour, IHear
{
    [SerializeField] private NavMeshAgent agent = null;

    [SerializeField, Tooltip("How far away, in meters, the agent will run from danger.")] 
    private float displacementFromDanger = 10f;

    private AI enemyAI;

    private bool CanNavigate => agent != null &&
        agent.isActiveAndEnabled && agent.isOnNavMesh;

    void Awake() 
    {
        enemyAI = GetComponentInParent<AI>();

        if (agent == null)
            agent = GetComponentInParent<NavMeshAgent>();

        if (agent == null)
            Debug.LogWarning(name + " doesn't have an agent!");
    }

    public void RespondToSound(Sound sound)
    {
        if (sound == null || !isActiveAndEnabled)
            return;

        if (enemyAI != null)
        {
            enemyAI.RespondToSound(sound);
            return;
        }

        if (sound.soundType == Sound.SoundType.Interesting)
            MoveTo(sound.pos);
        else if (sound.soundType == Sound.SoundType.Dangerous) //Must have this case so that it doesn't run away from the default sound type
        {
            Vector3 dir = (sound.pos - transform.position).normalized;
            MoveTo(transform.position - (dir * displacementFromDanger));
        }
        //else will do nothing in the case of Sounds with Default sound type
    }

    private void MoveTo(Vector3 pos) 
    {
        if (CanNavigate && agent.SetDestination(pos))
            agent.isStopped = false;
    }
}
