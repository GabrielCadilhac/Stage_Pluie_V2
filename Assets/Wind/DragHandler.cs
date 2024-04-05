using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Experimental.GraphView;

public class DragHandler : MonoBehaviour, IDragHandler
{
    private Vector2 _min, _max;

    private void Start()
    {
        Vector2 size = transform.parent.GetComponent<RectTransform>().sizeDelta;
        Vector2 pos = (Vector2) transform.parent.position;

        _min = pos - size / 2f;   
        _max = pos + size / 2f;   
    }

    public void OnDrag(PointerEventData eventData)
    {
        float x = Mathf.Clamp(eventData.position.x, _min.x, _max.x);
        float y = Mathf.Clamp(eventData.position.y, _min.y, _max.y);
        transform.position = new Vector3(x, y, 0f);
    }
}
