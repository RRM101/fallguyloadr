using Levels;
using Levels.Hoops;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace fallguyloadr.Fixes
{
    public class HoopsManagerReimplementation : MonoBehaviour
    {
        HoopsManager hoopsManager;

        void Start()
        {
            hoopsManager = GetComponent<HoopsManager>();

            SpawnHoops();
        }

        void SpawnHoops()
        {
            HashSet<HoopSlot> hoopSlots = new HashSet<HoopSlot>();
            while (hoopSlots.Count < hoopsManager._hoopsHolder.GetChildCount() * (hoopsManager._percentageHoopsActive / 100f))
            {
                hoopSlots.Add(hoopsManager._hoopsHolder.GetChild(UnityEngine.Random.Range(0, hoopsManager._hoopsHolder.GetChildCount())).gameObject.GetComponent<HoopSlot>());
            }

            foreach (HoopSlot hoopSlot in hoopSlots)
            {
                COMMON_Hoop hoop = Instantiate(hoopsManager._hoopPrefab, hoopSlot.transform).GetComponentInChildren<COMMON_Hoop>();

                hoop.gameObject.AddComponent<HoopReimplementation>();  
                hoopSlot.Hoop = hoop;
            }
        }
    }
}
