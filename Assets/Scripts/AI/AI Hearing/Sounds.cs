using System.Collections.Generic;
using UnityEngine;

namespace GamePlay
{
    public static class Sounds
    {
        public static void MakeSound(Sound sound)
        {
            Collider[] colliders = Physics.OverlapSphere(sound.pos, sound.range);
            var notified = new HashSet<IHear>();

            foreach (Collider collider in colliders)
            {
                // Child colliders can hear for the AI on their parent.
                IHear hearer = collider.GetComponentInParent<IHear>();
                if (hearer != null && notified.Add(hearer))
                    hearer.RespondToSound(sound);
            }
        }
    }
}