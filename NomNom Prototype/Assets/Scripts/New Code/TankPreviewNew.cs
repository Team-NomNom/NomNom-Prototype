using UnityEngine;
using System.Linq;

public class TankPreviewNew : MonoBehaviour
{
    public static TankPreviewNew Instance { get; private set; }

    [Header("Preview Settings")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private LayerMask previewLayer;
    [SerializeField] private float rotationSpeed = 20f;

    private GameObject currentPreview;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (currentPreview != null)
            currentPreview.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }

    public void ShowTankPreview(GameObject tankPrefab)
    {
        // ----- Clean any previous preview --------------
        if (currentPreview != null)
            Destroy(currentPreview);

        if (tankPrefab == null) return;

        // -------- Instantiate as child of the anchor --------------
        currentPreview = Instantiate(tankPrefab, previewAnchor);
        StartCoroutine(DebugTraceSpawn(currentPreview));
        currentPreview.transform.localPosition = Vector3.zero;
        currentPreview.transform.localRotation = Quaternion.identity;
        currentPreview.transform.localScale = Vector3.one;

        // ----- Strip networking + physics --------------
        foreach (var no in currentPreview.GetComponentsInChildren<Unity.Netcode.NetworkObject>())
            Destroy(no);
        foreach (var nb in currentPreview.GetComponentsInChildren<Unity.Netcode.NetworkBehaviour>())
            Destroy(nb);
        foreach (var col in currentPreview.GetComponentsInChildren<Collider>())
            Destroy(col);

        // ─── Put everything on the preview layer --------------
        int previewLayerIndex = LayerMaskToLayer(previewLayer);
        SetLayerRecursively(currentPreview, previewLayerIndex);
        Debug.Log($"[Preview] Applied preview layer = {previewLayerIndex} to {currentPreview.name}");

        // --- Auto-position camera --------------
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            Bounds b = GetRenderableBounds(currentPreview);
            float radius = b.extents.magnitude;
            float distance = Mathf.Max(1f, radius * 2.5f);
            Vector3 dir = new Vector3(0, 0.25f, 1).normalized;

            cam.transform.position = previewAnchor.position + dir * distance;
            cam.transform.LookAt(b.center + Vector3.up * (radius * 0.25f));
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = distance + radius * 2f;
        }

        Debug.Log($"[Preview] Spawned {currentPreview.name} under {previewAnchor.name}, layer = {previewLayerIndex}");
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private int LayerMaskToLayer(LayerMask mask)
    {
        int val = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((val & (1 << i)) != 0)
                return i;
        }
        Debug.LogWarning("[Preview] Could not determine layer from LayerMask");
        return 0;
    }

    private Bounds GetRenderableBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers.Skip(1))
            bounds.Encapsulate(r.bounds);

        return bounds;
    }
    public void ClearPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }

    private System.Collections.IEnumerator DebugTraceSpawn(GameObject go)
    {
        for (int f = 0; f < 120; f++)     // check for the next 2 seconds (120 frames @60 fps)
        {
            Debug.Log($"[Trace] Frame {f} → "
                    + (go == null ? "GO = null"
                                  : $"GO active={go.activeInHierarchy}, parent={go.transform.parent?.name}, layer={go.layer}"));

            yield return null;
        }
    }


}
