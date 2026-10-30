






















using UnityEngine;
using UnityEditor;

namespace Kino
{

    public class BloomGraphDrawer
    {
        #region Public Methods


        public void Prepare(Bloom bloom)
        {
            #if UNITY_5_6_OR_NEWER
            if (bloom.GetComponent<Camera>().allowHDR)
            #else
            if (bloom.GetComponent<Camera>().hdr)
            #endif
            {
                _rangeX = 6;
                _rangeY = 1.5f;
            }
            else
            {
                _rangeX = 1;
                _rangeY = 1;
            }

            _threshold = bloom.thresholdLinear;
            _knee = bloom.softKnee * _threshold + 1e-5f;


            _intensity = Mathf.Min(bloom.intensity, 10);
        }


        public void DrawGraph()
        {
            _rectGraph = GUILayoutUtility.GetRect(128, 80);


            DrawRect(0, 0, _rangeX, _rangeY, 0.1f, 0.4f);


            DrawRect(_threshold - _knee, 0, _threshold + _knee, _rangeY, 0.25f, -1);


            for (var i = 1; i < _rangeY; i++)
                DrawLine(0, i, _rangeX, i, 0.4f);


            for (var i = 1; i < _rangeX; i++)
                DrawLine(i, 0, i, _rangeY, 0.4f);


            Handles.Label(
                PointInRect(0, _rangeY) + Vector3.right,
                "Brightness Response (linear)", EditorStyles.miniLabel
            );


            DrawLine(_threshold, 0, _threshold, _rangeY, 0.6f);


            var vcount = 0;
            while (vcount < _curveResolution)
            {
                var x = _rangeX * vcount / (_curveResolution - 1);
                var y = ResponseFunction(x);
                if (y < _rangeY)
                {
                    _curveVertices[vcount++] = PointInRect(x, y);
                }
                else
                {
                    if (vcount > 1)
                    {

                        var v1 = _curveVertices[vcount - 2];
                        var v2 = _curveVertices[vcount - 1];
                        var clip = (_rectGraph.y - v1.y) / (v2.y - v1.y);
                        _curveVertices[vcount - 1] = v1 + (v2 - v1) * clip;
                    }
                    break;
                }
            }

            if (vcount > 1)
            {
                Handles.color = Color.white * 0.9f;
                Handles.DrawAAPolyLine(2.0f, vcount, _curveVertices);
            }
        }

        #endregion

        #region Response Function

        float _threshold;
        float _knee;
        float _intensity;

        float ResponseFunction(float x)
        {
            var rq = Mathf.Clamp(x - _threshold + _knee, 0, _knee * 2);
            rq = rq * rq * 0.25f / _knee;
            return Mathf.Max(rq, x - _threshold) * _intensity;
        }

        #endregion

        #region Graph Functions


        const int _curveResolution = 96;


        Vector3[] _rectVertices = new Vector3[4];
        Vector3[] _lineVertices = new Vector3[2];
        Vector3[] _curveVertices = new Vector3[_curveResolution];

        Rect _rectGraph;
        float _rangeX;
        float _rangeY;


        Vector3 PointInRect(float x, float y)
        {
            x = Mathf.Lerp(_rectGraph.x, _rectGraph.xMax, x / _rangeX);
            y = Mathf.Lerp(_rectGraph.yMax, _rectGraph.y, y / _rangeY);
            return new Vector3(x, y, 0);
        }


        void DrawLine(float x1, float y1, float x2, float y2, float grayscale)
        {
            _lineVertices[0] = PointInRect(x1, y1);
            _lineVertices[1] = PointInRect(x2, y2);
            Handles.color = Color.white * grayscale;
            Handles.DrawAAPolyLine(2.0f, _lineVertices);
        }


        void DrawRect(float x1, float y1, float x2, float y2, float fill, float line)
        {
            _rectVertices[0] = PointInRect(x1, y1);
            _rectVertices[1] = PointInRect(x2, y1);
            _rectVertices[2] = PointInRect(x2, y2);
            _rectVertices[3] = PointInRect(x1, y2);

            Handles.DrawSolidRectangleWithOutline(
                _rectVertices,
                fill < 0 ? Color.clear : Color.white * fill,
                line < 0 ? Color.clear : Color.white * line
            );
        }

        #endregion
    }
}
