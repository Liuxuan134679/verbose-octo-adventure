using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BossClicker
{
    public sealed class FireCadence
    {
        const double Epsilon = 1e-9;
        double nextShotAt = double.NegativeInfinity;
        bool held;

        public int Press(double now, double interval)
        {
            if (held || !Valid(interval)) return 0;
            held = true;
            if (now + Epsilon < nextShotAt) return 0;
            nextShotAt = now + interval;
            return 1;
        }

        public int Tick(double now, double interval)
        {
            if (!held || !Valid(interval)) return 0;
            int count = 0;
            while (now + Epsilon >= nextShotAt && count < 1024)
            {
                nextShotAt += interval;
                count++;
            }
            return count;
        }

        public void Release() => held = false;

        public void Reset()
        {
            held = false;
            nextShotAt = double.NegativeInfinity;
        }

        static bool Valid(double interval) => interval > 0 &&
            !double.IsNaN(interval) && !double.IsInfinity(interval);
    }

    public sealed class FireInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler
    {
        public GameView view;
        readonly FireCadence cadence = new FireCadence();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || view == null) return;
            FireDue(cadence.Press(Time.unscaledTimeAsDouble, view.CurrentFireInterval));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) cadence.Release();
        }

        public void OnPointerExit(PointerEventData eventData) => cadence.Release();

        void Update()
        {
            if (view == null) return;
            FireDue(cadence.Tick(Time.unscaledTimeAsDouble, view.CurrentFireInterval));
        }

        void OnDisable() => cadence.Reset();

        void OnApplicationFocus(bool focus)
        {
            if (!focus) cadence.Release();
        }

        void FireDue(int count)
        {
            for (int i = 0; i < count; i++)
                if (!view.Fire())
                {
                    cadence.Release();
                    break;
                }
        }
    }
}
