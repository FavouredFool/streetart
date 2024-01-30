using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ColorWheel : MonoBehaviour
{
    [SerializeField] public Transform projectedCursor;
    [SerializeField] public Transform raycastedCursor;
    [SerializeField] List<Color> colors = new List<Color>();
    private int _resolution = 200;
    public Color CurrentColor { get; private set; }
    private Color _hoverColor = Color.clear;
    private bool _active = false;

    private Texture2D _texture;

    // Start is called before the first frame update
    void Start()
    {
        CurrentColor = colors[0];
        _texture = new Texture2D(_resolution, _resolution);
        GetComponent<Renderer>().material.mainTexture = _texture;
        ClearTexture();
    }

    void ClearTexture()
    {
        for(int i = 0; i < _texture.width; i++)
        {
            for(int j = 0; j < _texture.height; j++)
            {
                _texture.SetPixel(i, j, Color.clear);
            }
        }
        _texture.Apply();
    }

    void UpdateUI()
    {
        float width = 360f / colors.Count;
        int idx = 0;
        DrawCircle(_texture, CurrentColor, _resolution/2, _resolution/2, radius: _resolution/10);
        foreach(Color c in colors)
        {
            DrawCircle(_texture, c, _resolution/2, _resolution/2, idx * width + 1, width - 2, _resolution/2 - (_hoverColor == c ? 0 : _resolution/20), _resolution/3);
            idx++;
        }
        _texture.Apply();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (!_active) return;
        transform.position = new Vector3(projectedCursor.transform.position.x, projectedCursor.transform.position.y, transform.position.z);
        float angle = Vector2.SignedAngle(new Vector2(0, -1), projectedCursor.transform.position - raycastedCursor.transform.position) + 180;
        var newColor = Vector2.Distance(projectedCursor.transform.position, raycastedCursor.transform.position) < 0.5 ? 
            Color.clear : colors[(int)(angle / (360f / colors.Count))];
        if(newColor != _hoverColor)
        {
            ClearTexture();
            _hoverColor = newColor;
            UpdateUI();
        }
    }

    public void ShowUI(bool show)
    {
        _active = show;
        if(!show)
            ClearTexture();
    }


    public void SelectColor()
    {
        if(_hoverColor != Color.clear)
            CurrentColor = _hoverColor;
    }

    public Texture2D DrawCircle(Texture2D tex, Color color, int x, int y, float angleLeft = 0, float angularwidth = 360, int radius = 3, int deadzone = 0)
    {
        // from: https://stackoverflow.com/questions/30410317/how-to-draw-circle-on-texture-in-unity

        float rSquared = radius * radius;
        int deadzoneSquared = deadzone * deadzone;
        for (int u = x - radius; u < x + radius + 1; u++)
            for (int v = y - radius; v < y + radius + 1; v++) {
                float angle = Vector2.SignedAngle(new Vector2(0, -1), new Vector2(u-x, v-y).normalized) + 180;
                int distanceSquared = (x - u) * (x - u) + (y - v) * (y - v);
                if (distanceSquared < rSquared && distanceSquared > deadzoneSquared && angle >= angleLeft && angle <= angleLeft + angularwidth)
                {
                    tex.SetPixel(u, v, color);
                }
            }
        return tex;
    }
}