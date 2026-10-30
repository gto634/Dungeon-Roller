using UnityEngine;

public class BrainSpike : MonoBehaviour
{

    [SerializeField] Clock _clock;

    [SerializeField] AttackSystem _attackSystem;

    [SerializeField] Transform _spikeTransform;
    [SerializeField] Animator _spikeAnimator;


    float _clockPreparation = 4f;
    float _clockAttack = 1f;

    enum state
    {
        SAFE,
        LOADED,
        ATTACK
    }
    state _state;


    void Start()
    {
        _state = state.SAFE;
        _clock.TimeClock = _clockPreparation;
        _clock.OnFinish += Attack;
        _attackSystem.Desactivated += Desactivate;
    }
    void OnDestroy()
    {
        _clock.OnFinish -= Attack;
    }

    void Attack()
    {
        switch(_state)
        {
            case state.SAFE:
                _spikeAnimator.SetBool("Activate",true);
                _state = state.LOADED;
                _clock.TimeClock = _clockAttack;
                _clock.ResetClock();
                return;

            case state.LOADED:
                _attackSystem.Attack();
                _spikeAnimator.SetBool("Attack",true);
                _state = state.ATTACK;
                _clock.TimeClock = _clockPreparation;
                _clock.ResetClock();
                return;

            default:
                break;
        }
    }

    void Desactivate()
    {
        _state = state.SAFE;
        _spikeAnimator.SetBool("Activate",false);
        _spikeAnimator.SetBool("Attack",false);
        _clock.ResetClock();
    }


}
