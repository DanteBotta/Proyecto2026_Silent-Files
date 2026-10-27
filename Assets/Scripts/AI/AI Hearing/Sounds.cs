using System.Collections.Generic;
using UnityEngine;

namespace GamePlay
{
    public static class Sounds
    {
        public static void MakeSound(Sound sound)
        {
            if (sound == null || sound.range <= 0f)
                return;

            Collider[] colliders = Physics.OverlapSphere(sound.pos, sound.range,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            var notified = new HashSet<IHear>();

            foreach (Collider collider in colliders)
            {
                // La IA y su adaptador NormalGuard deben contar como un solo receptor.
                IHear hearer = collider.GetComponentInParent<AI>();
                if (hearer == null)
                    hearer = collider.GetComponentInParent<IHear>();
                if (hearer != null && notified.Add(hearer))
                    hearer.RespondToSound(sound);
            }
        }
    }
}
