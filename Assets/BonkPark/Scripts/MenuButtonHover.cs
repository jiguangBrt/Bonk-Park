using UnityEngine;
using UnityEngine.EventSystems;

// Flanks a menu button with two ornaments while it is hovered or selected, so the active choice reads without any
// background block behind the label.
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] GameObject leftMark;
    [SerializeField] GameObject rightMark;

    void Awake() => SetMarks(false);
    void OnEnable() => SetMarks(false);

    public void OnPointerEnter(PointerEventData eventData) => SetMarks(true);
    public void OnPointerExit(PointerEventData eventData) => SetMarks(false);
    public void OnSelect(BaseEventData eventData) => SetMarks(true);
    public void OnDeselect(BaseEventData eventData) => SetMarks(false);

    void SetMarks(bool on)
    {
        if (leftMark != null) leftMark.SetActive(on);
        if (rightMark != null) rightMark.SetActive(on);
    }
}
