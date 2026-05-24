using UnityEngine;

// 把相机正交大小设成与 Park 的设计视野一致：orthographicSize = referenceViewport.y / 2
// （Unity 的 orthographicSize 是视口高度的一半）。这样无论分辨率多少，世界单位下看到的
// 内容总是 Park 设计的那一帧。横向被裁切 / 黑边交给 LetterboxCamera 处理。
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [Tooltip("提供 referenceViewport 的 Park 组件。一般直接拖场景里的 Park 物体。")]
    [SerializeField] Park park;

    void Start()
    {
        GetComponent<Camera>().orthographicSize = park.ReferenceViewport.y * 0.5f;
    }
}
