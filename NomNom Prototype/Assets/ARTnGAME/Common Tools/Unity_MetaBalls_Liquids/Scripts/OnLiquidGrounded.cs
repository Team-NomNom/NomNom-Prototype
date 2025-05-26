using UnityEngine;
namespace Artngame.CommonTools.Clay
{
    public class OnLiquidGrounded : MonoBehaviour
    {

        [SerializeField] private Animator FluidAnimator;
        public float scaleDown = 0.2f;

        void Start()
        {
            if (FluidAnimator == null)
            {
                FluidAnimator = GetComponent<Animator>();
            }
        }

        void OnCollisionEnter(Collision col)
        {
            if (FluidAnimator != null && col.gameObject.CompareTag("Ground"))
            {
                FluidAnimator.SetBool("IsGrounded", true);
            }
            if (this.transform.parent.transform.localScale.y > scaleDown)
            {
                this.transform.parent.transform.localScale -= new Vector3(0, this.transform.parent.transform.localScale.y * Time.deltaTime * 0.6f, 0);
            }
        }

        void OnCollisionExit(Collision col)
        {
            if (FluidAnimator != null && col.gameObject.CompareTag("Ground"))
            {
                FluidAnimator.SetBool("IsGrounded", false);
            }
        }
    }
}