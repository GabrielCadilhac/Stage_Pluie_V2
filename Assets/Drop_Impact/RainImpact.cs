using System;
using UnityEngine;

namespace Drop_Impact
{
	public class RainImpact
	{
        // Shader properties
        private static readonly int PositionArray = Shader.PropertyToID("Position");
		private static readonly int NormalArray   = Shader.PropertyToID("Normales");
		private static readonly int TimesArray    = Shader.PropertyToID("Times");
		private static readonly int AnimSpeed	  = Shader.PropertyToID("_AnimSpeed");
		private static readonly int DeltaTime     = Shader.PropertyToID("_DeltaTime");

		private ComputeBuffer _impPos, _impNor, _impTimes;
		 
		// Rendering
		private readonly RenderParams _rp;

		public RainImpact(Material pMaterial, Transform pTransform, int pNbParticles)
		{
            _rp = new(pMaterial)
            {
                matProps = new MaterialPropertyBlock(),
                worldBounds = new Bounds(pTransform.position, Vector3.one * 100f)
            };

            _impPos   = new ComputeBuffer(pNbParticles, 4 * sizeof(float));
            _impNor   = new ComputeBuffer(pNbParticles, 3 * sizeof(float));
            _impTimes = new ComputeBuffer(pNbParticles, sizeof(float));
            Graphics.SetRandomWriteTarget(1, _impTimes, true);

            Vector4[] tempPos = new Vector4[pNbParticles];
            Vector3[] tempNor = new Vector3[pNbParticles];
            float[] tempTimes = new float[pNbParticles];
            for (int i = 0; i < pNbParticles; i++)
            {
	            tempPos[i]   = Vector4.zero;
                tempNor[i]   = pTransform.up;
                tempTimes[i] = 0f;
            }

            _impPos.SetData(tempPos);
            _impNor.SetData(tempNor);
            _impTimes.SetData(tempTimes);

            _rp.material.SetBuffer(TimesArray, _impTimes);
            _rp.material.SetBuffer(PositionArray, _impPos);
            _rp.material.SetBuffer(NormalArray, _impNor);
        }

		public void Update(int pNbParticles, float pAnimSpeed)
		{
			_rp.material.SetFloat(DeltaTime, Time.deltaTime * pAnimSpeed);
			Graphics.RenderPrimitives(_rp, MeshTopology.Quads, 4, pNbParticles);
		}

		public ComputeBuffer GetPosBuffer()
		{
			return _impPos;
		}

		public ComputeBuffer GetNormalBuffer()
        {
            return _impNor;
        }

		public ComputeBuffer GetTimesBuffer()
        {
            return _impTimes;
        }
        public void Disable()
        {
            _impPos.Dispose();
            _impPos = null;

            _impNor.Dispose();
            _impNor = null;

            _impTimes.Dispose();
            _impTimes = null;
        }
    }
}
