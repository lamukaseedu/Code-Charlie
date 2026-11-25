/*
 * Author: Lam Nguyen
 * Created: 10/25/2026
 */

using UnityEngine;

// Passive access credentials checked by locks from any inventory slot.
public class Keycard : MonoBehaviour
{
    [Min(0)]
    [SerializeField] private int id = 0;

    public int Id => id;
}
