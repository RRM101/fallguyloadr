using DG.Tweening;
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

                hoop.gameObject.AddComponent<HoopReimplementation>().OnCollect = SpawnHoopInEmptyHoopSlot;  
                hoopSlot.Hoop = hoop;
            }
        }

        void SpawnHoopInEmptyHoopSlot()
        {
            List<HoopSlot> emptyHoopSlots = new();

            for (int i = 0; i < hoopsManager._hoopsHolder.GetChildCount(); i++)
            {
                HoopSlot hoopSlot = hoopsManager._hoopsHolder.GetChild(i).gameObject.GetComponent<HoopSlot>();
                if (hoopSlot.Hoop == null)
                {
                    emptyHoopSlots.Add(hoopSlot);
                }
            }

            HoopSlot randomEmptyHoopSlot = emptyHoopSlots[UnityEngine.Random.Range(0, emptyHoopSlots.Count())];

            COMMON_Hoop hoop = Instantiate(hoopsManager._hoopPrefab, randomEmptyHoopSlot.transform).GetComponentInChildren<COMMON_Hoop>();
            hoop.gameObject.AddComponent<HoopReimplementation>().OnCollect = SpawnHoopInEmptyHoopSlot;
            randomEmptyHoopSlot.Hoop = hoop;

            hoop.PlayEnter(false, 1, randomEmptyHoopSlot.transform.position, new Quaternion(), false);
            Vector3 pos = hoop.transform.parent.position;
            pos.y += 100;
            hoop.transform.parent.position = pos;
            hoop.transform.parent.DOMoveY(randomEmptyHoopSlot.transform.position.y, 2).SetEase(Ease.InOutSine);
        }
    }
}
