using BepInEx.Unity.IL2CPP.Utils.Collections;
using DG.Tweening;
using Levels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace fallguyloadr.Fixes
{
    public class HoopReimplementation : MonoBehaviour
    {
        COMMON_Hoop hoop;
        bool collected = false;
        public Action OnCollect;

        void Awake()
        {
            hoop = GetComponent<COMMON_Hoop>();
            hoop.RegisterRemoteMethods();
            hoop._normalMaterialPropertyBlock = new MaterialPropertyBlock();
            hoop._goldMaterialPropertyBlock = new MaterialPropertyBlock();

            //hoop.transform.localScale = new Vector3(1, 0.5f, 1);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<FallGuysCharacterController>() != null && !collected)
            {
                collected = true;
                hoop.ShowSoloVFX(false);
                StartCoroutine(Exit().WrapToIl2Cpp());
            }
        }

        IEnumerator Exit()
        {
            yield return new WaitForSeconds(1);
            Vector3 pos = transform.position;
            pos.y += 100;
            yield return transform.parent.DOMoveY(pos.y, 2).SetEase(Ease.InOutSine);
            OnCollect.Invoke();
            Destroy(this);
        }
    }
}