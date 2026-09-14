using UnityEngine;
using UnityEngine.EventSystems;

namespace BossClicker
{
    public sealed class FireInput : MonoBehaviour, IPointerDownHandler
    {
        public GameView view;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                view.Fire();
        }
    }
}
