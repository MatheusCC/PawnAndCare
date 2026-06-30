using UnityEngine;
using UnityEngine.Pool;
using PawsAndCare.Core;

namespace PawsAndCare.UI
{
    /// <summary>
    /// Spawns floating feedback popups from the deltas the economy and reputation systems already
    /// emit: a gain/loss pops near the matching status-bar element and rises away. Popups are pooled
    /// (reused, not destroyed) to avoid per-spawn allocation. Event-driven — nothing runs per frame
    /// here. Lives on its own (popup) canvas.
    /// </summary>
    public class FloatingPopupSpawner : MonoBehaviour
    {
        private const int DEFAULT_POOL_CAPACITY = 10;
        private const int MAX_POOL_SIZE = 40;

        [SerializeField]
        [Tooltip("Popup prefab — needs a FloatingPopup + CanvasGroup + TMP text.")]
        private FloatingPopup popupPrefab = null;

        [SerializeField]
        [Tooltip("Where money popups originate (place near the money readout). A plain RectTransform.")]
        private RectTransform moneyAnchor = null;

        [SerializeField]
        [Tooltip("Where reputation popups originate (place near the reputation readout).")]
        private RectTransform reputationAnchor = null;

        [SerializeField]
        [Tooltip("Random horizontal spread (pixels) so stacked popups don't perfectly overlap.")]
        private float horizontalJitter = 12.0f;

        [SerializeField]
        [Tooltip("Color for money gains (Honey Gold).")]
        private Color moneyGainColor = new Color(0.961f, 0.761f, 0.420f);

        [SerializeField]
        [Tooltip("Color for money losses / expenses (Coral).")]
        private Color moneyLossColor = new Color(0.882f, 0.439f, 0.333f);

        [SerializeField]
        [Tooltip("Color for reputation gains (Sage Green).")]
        private Color reputationGainColor = new Color(0.506f, 0.780f, 0.518f);

        [SerializeField]
        [Tooltip("Color for reputation losses (Coral).")]
        private Color reputationLossColor = new Color(0.882f, 0.439f, 0.333f);

        private ObjectPool<FloatingPopup> popupPool;

        private void Awake()
        {
            popupPool = new ObjectPool<FloatingPopup>(
                CreatePopup,
                OnGetPopup,
                OnReturnPopup,
                OnDestroyPopup,
                true,
                DEFAULT_POOL_CAPACITY,
                MAX_POOL_SIZE);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BalanceChangedEvent>(OnBalanceChanged);
            EventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BalanceChangedEvent>(OnBalanceChanged);
            EventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        private void OnBalanceChanged(BalanceChangedEvent eventData)
        {
            if (!Mathf.Approximately(eventData.Delta, 0.0f))
            {
                Color color = eventData.Delta >= 0.0f ? moneyGainColor : moneyLossColor;
                Spawn(moneyAnchor, FormatMoneyDelta(eventData.Delta), color);
            }
        }

        private void OnReputationChanged(ReputationChangedEvent eventData)
        {
            if (!Mathf.Approximately(eventData.Delta, 0.0f))
            {
                Color color = eventData.Delta >= 0.0f ? reputationGainColor : reputationLossColor;
                Spawn(reputationAnchor, FormatReputationDelta(eventData.Delta), color);
            }
        }

        private string FormatMoneyDelta(float delta)
        {
            string sign = delta >= 0.0f ? "+" : "-";

            return $"{sign}{MoneyFormatUtils.Format(Mathf.Abs(delta))}";
        }

        private string FormatReputationDelta(float delta)
        {
            string sign = delta >= 0.0f ? "+" : "-";

            return $"{sign}{Mathf.Abs(delta):0} rep";
        }

        private void Spawn(RectTransform anchor, string text, Color color)
        {
            if (popupPrefab != null && anchor != null)
            {
                FloatingPopup popup = popupPool.Get();
                RectTransform popupRect = (RectTransform)popup.transform;
                popupRect.SetParent(anchor, false);
                popupRect.anchoredPosition = new Vector2(Random.Range(-horizontalJitter, horizontalJitter), 0.0f);
                popup.Play(text, color, ReturnPopupToPool);
            }
            else
            {
                Debug.LogError("[FloatingPopupSpawner] popupPrefab or anchor is missing — assign them in the inspector.", this);
            }
        }

        private void ReturnPopupToPool(FloatingPopup popup)
        {
            popupPool.Release(popup);
        }

        // Pool plumbing: create parents under the spawner; get/release toggle the active state;
        // destroy is only hit for instances released beyond MAX_POOL_SIZE.
        private FloatingPopup CreatePopup()
        {
            return Instantiate(popupPrefab, transform);
        }

        private void OnGetPopup(FloatingPopup popup)
        {
            popup.gameObject.SetActive(true);
        }

        private void OnReturnPopup(FloatingPopup popup)
        {
            popup.gameObject.SetActive(false);
        }

        private void OnDestroyPopup(FloatingPopup popup)
        {
            Destroy(popup.gameObject);
        }
    }
}
