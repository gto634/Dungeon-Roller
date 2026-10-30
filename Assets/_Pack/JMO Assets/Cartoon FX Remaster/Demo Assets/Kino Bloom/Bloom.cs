



























using UnityEngine;

namespace Kino
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	[ImageEffectAllowedInSceneView]
	public class Bloom : MonoBehaviour
	{
		#region Public Properties



		public float thresholdGamma
		{
			get { return Mathf.Max(_threshold, 0); }
			set { _threshold = value; }
		}



		public float thresholdLinear
		{
			get { return GammaToLinear(thresholdGamma); }
			set { _threshold = LinearToGamma(value); }
		}

		[SerializeField]
		float _threshold = 0.8f;



		public float softKnee
		{
			get { return _softKnee; }
			set { _softKnee = value; }
		}

		[SerializeField, Range(0, 1)]
		float _softKnee = 0.5f;




		public float radius
		{
			get { return _radius; }
			set { _radius = value; }
		}

		[SerializeField, Range(1, 7)]
		float _radius = 2.5f;



		public float intensity
		{
			get { return Mathf.Max(_intensity, 0); }
			set { _intensity = value; }
		}

		[SerializeField]
		float _intensity = 0.8f;



		public bool highQuality
		{
			get { return _highQuality; }
			set { _highQuality = value; }
		}

		[SerializeField]
		bool _highQuality = true;



		[SerializeField]
		bool _antiFlicker = true;

		public bool antiFlicker
		{
			get { return _antiFlicker; }
			set { _antiFlicker = value; }
		}

		#endregion

		#region Private Members

#pragma warning disable 0649
		[SerializeField, HideInInspector]
		Shader _shader;
#pragma warning restore 0649

		Material _material;

		const int kMaxIterations = 16;
		RenderTexture[] _blurBuffer1 = new RenderTexture[kMaxIterations];
		RenderTexture[] _blurBuffer2 = new RenderTexture[kMaxIterations];

		float LinearToGamma(float x)
		{
#if UNITY_5_3_OR_NEWER
			return Mathf.LinearToGammaSpace(x);
#else
			if (x <= 0.0031308f)
				return 12.92f * x;
			else
				return 1.055f * Mathf.Pow(x, 1 / 2.4f) - 0.055f;
#endif
		}

		float GammaToLinear(float x)
		{
#if UNITY_5_3_OR_NEWER
			return Mathf.GammaToLinearSpace(x);
#else
			if (x <= 0.04045f)
				return x / 12.92f;
			else
				return Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
#endif
		}

		#endregion

		#region MonoBehaviour Functions

		void OnEnable()
		{
			var shader = _shader ? _shader : Shader.Find("Hidden/Kino/Bloom");
			_material = new Material(shader);
			_material.hideFlags = HideFlags.DontSave;
		}

		void OnDisable()
		{
			DestroyImmediate(_material);
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			var useRGBM = Application.isMobilePlatform;


			var tw = source.width;
			var th = source.height;


			if (!_highQuality)
			{
				tw /= 2;
				th /= 2;
			}


			var rtFormat = useRGBM ?
				RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR;


			var logh = Mathf.Log(th, 2) + _radius - 8;
			var logh_i = (int)logh;
			var iterations = Mathf.Clamp(logh_i, 1, kMaxIterations);


			var lthresh = thresholdLinear;
			_material.SetFloat("_Threshold", lthresh);

			var knee = lthresh * _softKnee + 1e-5f;
			var curve = new Vector3(lthresh - knee, knee * 2, 0.25f / knee);
			_material.SetVector("_Curve", curve);

			var pfo = !_highQuality && _antiFlicker;
			_material.SetFloat("_PrefilterOffs", pfo ? -0.5f : 0.0f);

			_material.SetFloat("_SampleScale", 0.5f + logh - logh_i);
			_material.SetFloat("_Intensity", intensity);


			var prefiltered = RenderTexture.GetTemporary(tw, th, 0, rtFormat);
			var pass = _antiFlicker ? 1 : 0;
			Graphics.Blit(source, prefiltered, _material, pass);


			var last = prefiltered;
			for (var level = 0; level < iterations; level++)
			{
				_blurBuffer1[level] = RenderTexture.GetTemporary(
					last.width / 2, last.height / 2, 0, rtFormat
				);

				pass = (level == 0) ? (_antiFlicker ? 3 : 2) : 4;
				Graphics.Blit(last, _blurBuffer1[level], _material, pass);

				last = _blurBuffer1[level];
			}


			for (var level = iterations - 2; level >= 0; level--)
			{
				var basetex = _blurBuffer1[level];
				_material.SetTexture("_BaseTex", basetex);

				_blurBuffer2[level] = RenderTexture.GetTemporary(
					basetex.width, basetex.height, 0, rtFormat
				);

				pass = _highQuality ? 6 : 5;
				Graphics.Blit(last, _blurBuffer2[level], _material, pass);
				last = _blurBuffer2[level];
			}


			_material.SetTexture("_BaseTex", source);
			pass = _highQuality ? 8 : 7;
			Graphics.Blit(last, destination, _material, pass);


			for (var i = 0; i < kMaxIterations; i++)
			{
				if (_blurBuffer1[i] != null)
					RenderTexture.ReleaseTemporary(_blurBuffer1[i]);

				if (_blurBuffer2[i] != null)
					RenderTexture.ReleaseTemporary(_blurBuffer2[i]);

				_blurBuffer1[i] = null;
				_blurBuffer2[i] = null;
			}

			RenderTexture.ReleaseTemporary(prefiltered);
		}

		#endregion
	}
}
