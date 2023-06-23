using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEditor.Rendering;

[RequireComponent(typeof(PlayerInput))]
public class EditorController : MonoBehaviour
{
    [Header("Control")]
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private float sensitivityX = 15f;
    [SerializeField] private float sensitivityY = 15f;
    private int gridXZ = 6;
    private int gridY = 1;
    public static int nextTileID = 0;
    private int planeHeight = 0;
    private int rotateAmount = 45;
    private int scaleAmount = 5;
    private int transformationIndex = 0;

    [Header("Reference")]
    [SerializeField] private GameObject infoMenuCanvas;
    [SerializeField] private GameObject placeCursor;
    [SerializeField] private GameObject[] transformPrefabs = new GameObject[3];
    [SerializeField] private LayerMask pointLayer;
    [SerializeField] private LayerMask outlineLayer;

    private BlockTile selectedTile = null;
    private bool cantLook = true;
    private bool isClickPressedL = false;
    private bool pointOverGameObj = false;
    private bool pressingCTRL = false;
    private Camera mainCam;
    private GameObject[] transformObjs = new GameObject[3];
    private Transform selectedBlock = null;
    private Vector2 lookInput;
    private Vector2 mousePosition;
    private Vector2 rotation;
    private Vector3 moveInput;
    private Vector3 selectedOffset;
    private GameObject outlineBlock;

    public static event Action<PlacedTile> onPlacedTile;
    public static event Action<int> onTileDeleted;
    public static event Action<int, Transform> onUpdatePlacedTile;

    private void Start()
    {
        mainCam = GetComponent<Camera>();
        for (int i = 0; i < transformObjs.Length; i++) {
            transformObjs[i] = Instantiate(transformPrefabs[i], Vector3.zero, Quaternion.identity);
            transformObjs[i].SetActive(false);
        }
    }

    private void OnEnable()
    {
        EditorUI.onBlockSelected += TileSelected;
        InfoUI.onGridXZChange += GridXZChanged;
        InfoUI.onGridYChange += GridYChanged;
        InfoUI.onRotateChange += RotateChanged;
        InfoUI.onScaleChange += ScaleChanged;
        InfoUI.onTransformationChange += TransfomrationChanged;
    }
    private void OnDisable()
    {
        EditorUI.onBlockSelected -= TileSelected;
        InfoUI.onGridXZChange -= GridXZChanged;
        InfoUI.onGridYChange -= GridYChanged;
        InfoUI.onRotateChange -= RotateChanged;
        InfoUI.onScaleChange -= ScaleChanged;
        InfoUI.onTransformationChange -= TransfomrationChanged;
    }

    private void Update()
    {
        transform.Translate(moveInput * moveSpeed * Time.deltaTime);

        if (Mouse.current.leftButton.wasPressedThisFrame)
            pointOverGameObj = EventSystem.current.IsPointerOverGameObject(PointerInputModule.kMouseLeftId);

        LookHandler();
        PlaceHandler();
        SelectedMove();
    }

    private void SelectedBlock()
    {
        if (selectedBlock.transform.CompareTag("Transform") || selectedBlock.transform.CompareTag("Rotate") || selectedBlock.transform.CompareTag("Scale"))
            return;

        for (int i = 0; i < transformObjs.Length; i++) {
            transformObjs[i].transform.SetParent(selectedBlock.transform);
            transformObjs[i].transform.position = selectedBlock.transform.position;
            transformObjs[i].transform.localScale = selectedBlock.transform.localScale * 0.1f;
        }

        transformObjs[transformationIndex].SetActive(true);
    }
    private void ResetTransforms()
    {
        for (int i = 0; i < transformObjs.Length; i++) {
            transformObjs[i].transform.SetParent(null);
            transformObjs[i].transform.position = Vector3.zero;
            transformObjs[i].transform.localScale = Vector3.one;
        }

        transformObjs[transformationIndex].SetActive(false);
    }
    private void UpdateBlock(GameObject block) => onUpdatePlacedTile?.Invoke(block.GetComponent<TileInfo>().runtimeID, block.transform);
    private void PlaceHandler()
    {
        if (!cantLook) return;

        Ray ray = mainCam.ScreenPointToRay(mousePosition);
        float planeDistance = (planeHeight - ray.origin.y) / ray.direction.y;
        Vector3 cursorPosition = ray.GetPoint(planeDistance);

        cursorPosition.x = RoundToNearestGrid(cursorPosition.x, gridXZ);
        cursorPosition.y = RoundToNearestGrid(cursorPosition.y, gridY);
        cursorPosition.z = RoundToNearestGrid(cursorPosition.z, gridXZ);

        placeCursor.transform.position = cursorPosition;
        OutlineHandler(ray);
    }

    private void OutlineHandler(Ray ray)
    {
        if (selectedBlock != null && (selectedBlock.transform.CompareTag("Transform") || selectedBlock.transform.CompareTag("Rotate") || selectedBlock.transform.CompareTag("Scale")))
            return;

        RaycastHit hit;
        bool hitBlock = Physics.Raycast(ray, out hit, mainCam.farClipPlane, pointLayer);

        if (outlineBlock != null && ((1 << outlineBlock.layer) & outlineLayer) != 0 && hit.transform?.gameObject != outlineBlock) {
            outlineBlock.layer = LayerMask.NameToLayer("Default");
            outlineBlock = null;
        }

        if (hitBlock && outlineBlock != hit.transform.gameObject) {
            outlineBlock = hit.transform.gameObject;
            outlineBlock.layer = LayerMask.NameToLayer("Outline");
        }
    }

    private void TileSelected(BlockTile tile)
    {
        selectedTile = tile;
        placeCursor.GetComponent<MeshFilter>().mesh = tile.prefab.GetComponent<MeshFilter>().sharedMesh;
        placeCursor.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", tile.prefab.GetComponent<MeshRenderer>().sharedMaterial.mainTexture);
        placeCursor.transform.localScale = tile.prefab.transform.localScale;
    }

    private void SelectedMove()
    {
        if (isClickPressedL && selectedBlock != null && selectedBlock.transform.CompareTag("Transform")) {
            Directions selectedDirection = selectedBlock.GetComponent<TransformController>().direction;
            Ray ray = mainCam.ScreenPointToRay(mousePosition);
            Vector3 cursorPosition = selectedBlock.transform.parent.transform.position;
            Vector3 planeDistance = selectedBlock.transform.parent.transform.position - ray.origin;
            planeDistance.x /= ray.direction.x;
            planeDistance.y /= ray.direction.y;
            planeDistance.z /= ray.direction.z;
            Vector3 desiredPosition = cursorPosition;

            if ((Directions.X & selectedDirection) != 0) {
                cursorPosition.x = (Directions.Y & selectedDirection) != 0 ? ray.GetPoint(planeDistance.z).x : ray.GetPoint(planeDistance.y).x;
                cursorPosition.x += selectedOffset.x;
                desiredPosition.x = RoundToNearestGrid(cursorPosition.x, gridXZ);
            }
            if ((Directions.Y & selectedDirection) != 0) {
                cursorPosition.y = (Directions.Z & selectedDirection) != 0 ? ray.GetPoint(planeDistance.x).y : ray.GetPoint(planeDistance.z).y;
                cursorPosition.y += selectedOffset.y;
                desiredPosition.y = RoundToNearestGrid(cursorPosition.y, gridY);
            }
            if ((Directions.Z & selectedDirection) != 0) {
                cursorPosition.z = (Directions.X & selectedDirection) != 0 ? ray.GetPoint(planeDistance.y).z : ray.GetPoint(planeDistance.x).z;
                cursorPosition.z += selectedOffset.z;
                desiredPosition.z = RoundToNearestGrid(cursorPosition.z, gridXZ);
            }

            transformObjs[transformationIndex].transform.parent.transform.position = desiredPosition;
        }
        else if (isClickPressedL && selectedBlock != null && selectedBlock.transform.CompareTag("Scale")) {
            Vector2 scaleDifference = mousePosition - new Vector2(selectedOffset.x, selectedOffset.y);
            float desiredScale = (scaleDifference.x + scaleDifference.y) * 0.01f * scaleAmount;
            desiredScale = RoundToNearestGrid(desiredScale, scaleAmount);
            transformObjs[transformationIndex].transform.parent.transform.localScale = Vector3.one * (selectedOffset.z + desiredScale);
        }
        else if (isClickPressedL && selectedBlock != null && selectedBlock.transform.CompareTag("Rotate")) {
            Directions selectedDirection = selectedBlock.GetComponent<TransformController>().direction;
            Vector2 rotateDifference = new Vector3(mousePosition.x, mousePosition.y, 0) - mainCam.ScreenToWorldPoint(selectedOffset);
            float desiredAngle = (rotateDifference.x + rotateDifference.y) * 0.5f;
            Quaternion desiredRotation = Quaternion.identity;
            desiredAngle = RoundToNearestGrid(desiredAngle, rotateAmount);

            if ((Directions.X & selectedDirection) != 0)
                desiredRotation = Quaternion.Euler(-desiredAngle, 0f, 0f);
            else if ((Directions.Y & selectedDirection) != 0)
                desiredRotation = Quaternion.Euler(0f, -desiredAngle, 0f);
            else if ((Directions.Z & selectedDirection) != 0)
                desiredRotation = Quaternion.Euler(0f, 0f, -desiredAngle);

            transformObjs[transformationIndex].transform.parent.transform.rotation = desiredRotation;
        }
        else {
            selectedBlock = null;
        }
    }

    private void LookHandler()
    {
        if (cantLook) return;

        rotation.y += (lookInput.x * Time.deltaTime * sensitivityY);
        rotation.x -= (lookInput.y * Time.deltaTime * sensitivityX);
        rotation.x = Mathf.Clamp(rotation.x, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotation.x, rotation.y, 0);
    }

    private float RoundToNearestGrid(float value, float snapValue) => snapValue * Mathf.Round(value / snapValue);

    private RaycastHit ContainsTransform(RaycastHit[] hits)
    {
        for (int i = 0; i < hits.Length; i++) {
            if (hits[i].transform.CompareTag("Transform") || hits[i].transform.CompareTag("Rotate") || hits[i].transform.CompareTag("Scale"))
                return hits[i];
        }
        return hits[0];
    }

    private void GridXZChanged(int value) => gridXZ = value;
    private void GridYChanged(int value) => gridY = value;
    private void RotateChanged(int value) => rotateAmount = value;
    private void ScaleChanged(int value) => scaleAmount = value;
    private void TransfomrationChanged(Transformation value)
    {
        int activeIndex = 10;
        for (int i = 0; i < transformObjs.Length; i++) {
            if (transformObjs[i].activeInHierarchy)
                activeIndex = i;
        }

        if (activeIndex == 10) return;

        for (int i = 0; i < transformObjs.Length; i++) {
            if (transformObjs[i].CompareTag(value.ToString())) {
                transformationIndex = i;
                if (activeIndex != transformationIndex) {
                    transformObjs[activeIndex].SetActive(false);
                    transformObjs[transformationIndex].SetActive(true);
                }
            }
        }
    }

    private void OnLeftClick(InputValue value)
    {

        if (selectedBlock != null && isClickPressedL)
            UpdateBlock(selectedBlock.root.gameObject);

        isClickPressedL = value.Get<float>() == 1;

        if (selectedTile == null) {
            Ray ray = mainCam.ScreenPointToRay(mousePosition);
            RaycastHit[] selectedHits = Physics.RaycastAll(ray, mainCam.farClipPlane, pointLayer);
            if (selectedHits.Length > 0 && isClickPressedL) {
                RaycastHit hit = ContainsTransform(selectedHits);
                selectedBlock = hit.transform;
                if (hit.transform.CompareTag("Transform") || hit.transform.CompareTag("Rotate") || hit.transform.CompareTag("Scale")) {
                    if (hit.transform.CompareTag("Transform")) 
                        selectedOffset = hit.transform.parent.transform.position - hit.point;
                    else if (hit.transform.CompareTag("Scale")) 
                        selectedOffset = new Vector3(mousePosition.x, mousePosition.y, hit.transform.root.transform.localScale.x);
                    else if (hit.transform.CompareTag("Rotate")) 
                        selectedOffset = new Vector3(mousePosition.x, mousePosition.y, 0);
                }
                else
                    SelectedBlock();
            }
        }

        if (isClickPressedL) return;
        if (selectedTile == null) return;
        if (pointOverGameObj) return;

        GameObject tile = Instantiate(selectedTile.prefab, placeCursor.transform.position, placeCursor.transform.rotation);
        tile.AddComponent<TileInfo>().runtimeID = nextTileID;
        onPlacedTile?.Invoke(new PlacedTile(nextTileID, selectedTile.id, tile.transform));
        nextTileID++;

        if (pressingCTRL) return;
        placeCursor.GetComponent<MeshFilter>().mesh = null;
        selectedTile = null;
    }
    private void OnDelete(InputValue value)
    {
        if (transformObjs[transformationIndex].transform.parent == null)
            return;

        GameObject blockToDelete = transformObjs[transformationIndex].transform.parent.gameObject;
        onTileDeleted.Invoke(blockToDelete.GetComponent<TileInfo>().runtimeID);
        ResetTransforms();
        Destroy(blockToDelete);
    }
    private void OnCTRL(InputValue value) => pressingCTRL = value.Get<float>() == 1;
    private void OnScroll(InputValue value) => planeHeight += (int)Mathf.Sign(value.Get<float>()) * gridY;
    private void OnPlacePoint(InputValue value) => mousePosition = value.Get<Vector2>();
    private void OnRotate(InputValue value) => placeCursor.transform.Rotate(0, rotateAmount * value.Get<float>(), 0, Space.Self);
    private void OnMove(InputValue value) => moveInput = value.Get<Vector3>();
    private void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    private void OnRightClick(InputValue value) => cantLook = value.Get<float>() == 0;
    private void OnInfoMenu() => infoMenuCanvas.SetActive(!infoMenuCanvas.activeInHierarchy);
}