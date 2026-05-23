using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    public enum PanelAnimationType
    {
        FadeOnly, FadeScale,
        SlideFromBottom, SlideFromTop, SlideFromLeft, SlideFromRight,
        PopBounce, ElasticFromBottom, ElasticFromTop,
        FlipHorizontal, FlipVertical,
        RotateFadeIn, ZoomOut, ShakeAppear, SwingIn,
    }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _content;
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private PanelAnimationType _animationType = PanelAnimationType.FadeScale;

    [Header("Back Button")]
    [SerializeField] private UnityEvent _onBackPressed;

    private Vector2 _originalAnchoredPos;
    private Vector2 _slideOffset;
    private Action _showAction;
    private Action _hideAction;

    private static readonly Stack<UIPanel> _panelStack = new Stack<UIPanel>();
    private static readonly HashSet<UIPanel> _panelSet = new HashSet<UIPanel>();

    private static UIPanel _backListenerRegistered;

    // -------------------------------------------------------
    #region Unity Lifecycle

    private void Awake()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_content == null) _content = GetComponent<RectTransform>();

        _originalAnchoredPos = _content.anchoredPosition;
        InitSlideOffset();
        InitActions();
    }

    private void OnEnable() => Application.onBeforeRender += HandleBackInput;

    private void OnDisable() => Application.onBeforeRender -= HandleBackInput;

    private void OnDestroy() => RemoveFromStack();

    #endregion
    // -------------------------------------------------------
    #region Init

    private void InitSlideOffset()
    {
        _slideOffset = _animationType switch
        {
            PanelAnimationType.SlideFromBottom or PanelAnimationType.ElasticFromBottom
                => new Vector2(0, -Screen.height),
            PanelAnimationType.SlideFromTop or PanelAnimationType.ElasticFromTop
                => new Vector2(0, Screen.height),
            PanelAnimationType.SlideFromLeft
                => new Vector2(-Screen.width, 0),
            PanelAnimationType.SlideFromRight
                => new Vector2(Screen.width, 0),
            _ => Vector2.zero
        };
    }

    private void InitActions()
    {
        switch (_animationType)
        {
            case PanelAnimationType.FadeOnly:
                _showAction = PlayFadeOnlyShow; _hideAction = PlayFadeOnlyHide; break;
            case PanelAnimationType.FadeScale:
                _showAction = PlayFadeScaleShow; _hideAction = PlayFadeScaleHide; break;
            case PanelAnimationType.SlideFromBottom:
            case PanelAnimationType.SlideFromTop:
            case PanelAnimationType.SlideFromLeft:
            case PanelAnimationType.SlideFromRight:
                _showAction = () => PlaySlideShow(_slideOffset);
                _hideAction = () => PlaySlideHide(_slideOffset); break;
            case PanelAnimationType.PopBounce:
                _showAction = PlayPopBounceShow; _hideAction = PlayPopBounceHide; break;
            case PanelAnimationType.ElasticFromBottom:
            case PanelAnimationType.ElasticFromTop:
                _showAction = () => PlayElasticSlideShow(_slideOffset);
                _hideAction = () => PlayElasticSlideHide(_slideOffset); break;
            case PanelAnimationType.FlipHorizontal:
                _showAction = PlayFlipHorizontalShow; _hideAction = PlayFlipHorizontalHide; break;
            case PanelAnimationType.FlipVertical:
                _showAction = PlayFlipVerticalShow; _hideAction = PlayFlipVerticalHide; break;
            case PanelAnimationType.RotateFadeIn:
                _showAction = PlayRotateFadeInShow; _hideAction = PlayRotateFadeInHide; break;
            case PanelAnimationType.ZoomOut:
                _showAction = PlayZoomOutShow; _hideAction = PlayZoomOutHide; break;
            case PanelAnimationType.ShakeAppear:
                _showAction = PlayShakeAppearShow; _hideAction = PlayFadeOnlyHide; break;
            case PanelAnimationType.SwingIn:
                _showAction = PlaySwingInShow; _hideAction = PlaySwingInHide; break;
            default:
                _showAction = PlayFadeOnlyShow; _hideAction = PlayFadeOnlyHide; break;
        }
    }

    #endregion
    // -------------------------------------------------------
    #region Back Input (static, один обработчик на всё а то вонючка будет)

    private static void HandleBackInput()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (_panelStack.Count > 0) _panelStack.Peek()._onBackPressed?.Invoke();
    }

    #endregion
    // -------------------------------------------------------
    #region Stack Management

    private void PushToStack()
    {
        if (!_panelSet.Add(this)) return;
        _panelStack.Push(this);
    }

    private void RemoveFromStack()
    {
        if (!_panelSet.Remove(this)) return;

        var temp = new List<UIPanel>(_panelStack);
        _panelStack.Clear();
        for (int i = temp.Count - 1; i >= 0; i--)
        {
            if (temp[i] != this) _panelStack.Push(temp[i]);
        }
    }

    #endregion
    // -------------------------------------------------------
    #region Show / Hide

    public void Show()
    {
        DOTween.Kill(this);
        gameObject.SetActive(true);

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _content.localScale = Vector3.one;
        _content.anchoredPosition = _originalAnchoredPos;
        _content.localEulerAngles = Vector3.zero;

        PushToStack();
        _showAction?.Invoke();
    }

    public void Hide()
    {
        DOTween.Kill(this);

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        RemoveFromStack();
        _hideAction?.Invoke();
    }

    #endregion
    // -------------------------------------------------------
    #region Animations

    private void PlayFadeOnlyShow()
    {
        _canvasGroup.DOFade(1f, _duration).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlayFadeOnlyHide()
    {
        _canvasGroup.DOFade(0f, _duration).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlayFadeScaleShow()
    {
        _content.localScale = Vector3.one * 0.8f;
        _canvasGroup.DOFade(1f, _duration).SetUpdate(true).SetId(this);
        _content.DOScale(1f, _duration).SetEase(Ease.OutBack).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlayFadeScaleHide()
    {
        _canvasGroup.DOFade(0f, _duration).SetUpdate(true).SetId(this);
        _content.DOScale(0.8f, _duration).SetEase(Ease.InBack).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlaySlideShow(Vector2 offset)
    {
        _content.anchoredPosition = _originalAnchoredPos + offset;
        _canvasGroup.DOFade(1f, _duration).SetUpdate(true).SetId(this);
        _content.DOAnchorPos(_originalAnchoredPos, _duration).SetEase(Ease.OutCubic).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlaySlideHide(Vector2 offset)
    {
        _canvasGroup.DOFade(0f, _duration).SetUpdate(true).SetId(this);
        _content.DOAnchorPos(_originalAnchoredPos + offset, _duration).SetEase(Ease.InCubic).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlayElasticSlideShow(Vector2 offset)
    {
        _content.anchoredPosition = _originalAnchoredPos + offset;
        _canvasGroup.DOFade(1f, _duration * 0.5f).SetUpdate(true).SetId(this);
        _content.DOAnchorPos(_originalAnchoredPos, _duration).SetEase(Ease.OutElastic, 1f, 0.4f).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlayElasticSlideHide(Vector2 offset)
    {
        _canvasGroup.DOFade(0f, _duration * 0.4f).SetUpdate(true).SetId(this);
        _content.DOAnchorPos(_originalAnchoredPos + offset, _duration * 0.7f).SetEase(Ease.InBack).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlayPopBounceShow()
    {
        _content.localScale = Vector3.one * 0.5f;
        _canvasGroup.DOFade(1f, _duration * 0.7f).SetUpdate(true).SetId(this);
        _content.DOScale(1f, _duration).SetEase(Ease.OutElastic, 1f, 0.3f).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlayPopBounceHide()
    {
        float d = _duration * 0.6f;
        _canvasGroup.DOFade(0f, d).SetUpdate(true).SetId(this);
        _content.DOScale(0.5f, d).SetEase(Ease.InBack).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlayFlipHorizontalShow()
    {
        _content.localEulerAngles = new Vector3(0f, 90f, 0f);
        _canvasGroup.DOFade(1f, _duration * 0.3f).SetDelay(_duration * 0.4f).SetUpdate(true).SetId(this);
        _content.DOLocalRotate(Vector3.zero, _duration).SetEase(Ease.OutBack).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlayFlipHorizontalHide()
    {
        _canvasGroup.DOFade(0f, _duration * 0.3f).SetDelay(_duration * 0.5f).SetUpdate(true).SetId(this);
        _content.DOLocalRotate(new Vector3(0f, 90f, 0f), _duration).SetEase(Ease.InBack).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlayFlipVerticalShow()
    {
        _content.localEulerAngles = new Vector3(90f, 0f, 0f);
        _canvasGroup.DOFade(1f, _duration * 0.3f).SetDelay(_duration * 0.4f).SetUpdate(true).SetId(this);
        _content.DOLocalRotate(Vector3.zero, _duration).SetEase(Ease.OutBack).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlayFlipVerticalHide()
    {
        _canvasGroup.DOFade(0f, _duration * 0.3f).SetDelay(_duration * 0.5f).SetUpdate(true).SetId(this);
        _content.DOLocalRotate(new Vector3(90f, 0f, 0f), _duration).SetEase(Ease.InBack).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlayRotateFadeInShow()
    {
        _content.localEulerAngles = new Vector3(0f, 0f, -15f);
        _content.localScale = Vector3.one * 0.85f;
        _canvasGroup.DOFade(1f, _duration).SetUpdate(true).SetId(this);
        _content.DOLocalRotate(Vector3.zero, _duration).SetEase(Ease.OutCubic).SetUpdate(true).SetId(this);
        _content.DOScale(1f, _duration).SetEase(Ease.OutBack).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlayRotateFadeInHide()
    {
        _canvasGroup.DOFade(0f, _duration).SetUpdate(true).SetId(this);
        _content.DOLocalRotate(new Vector3(0f, 0f, 15f), _duration).SetEase(Ease.InCubic).SetUpdate(true).SetId(this);
        _content.DOScale(0.85f, _duration).SetEase(Ease.InBack).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlayZoomOutShow()
    {
        _content.localScale = Vector3.one * 1.3f;
        _canvasGroup.DOFade(1f, _duration).SetUpdate(true).SetId(this);
        _content.DOScale(1f, _duration).SetEase(Ease.OutCubic).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlayZoomOutHide()
    {
        _canvasGroup.DOFade(0f, _duration).SetUpdate(true).SetId(this);
        _content.DOScale(1.3f, _duration).SetEase(Ease.InCubic).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    private void PlayShakeAppearShow()
    {
        _content.localScale = Vector3.one * 0.9f;
        _canvasGroup.DOFade(1f, _duration * 0.3f).SetUpdate(true).SetId(this).OnComplete(() =>
        {
            _content.DOScale(1f, _duration * 0.2f).SetEase(Ease.OutBack).SetUpdate(true).SetId(this).OnComplete(() =>
            {
                _content.DOShakePosition(_duration * 0.6f, 18f, 10, 45).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
            });
        });
    }

    private void PlaySwingInShow()
    {
        _content.localEulerAngles = new Vector3(0f, 0f, 25f);
        _content.anchoredPosition = _originalAnchoredPos + new Vector2(0, Screen.height * 0.3f);
        _canvasGroup.DOFade(1f, _duration * 0.5f).SetUpdate(true).SetId(this);
        _content.DOAnchorPos(_originalAnchoredPos, _duration).SetEase(Ease.OutBounce).SetUpdate(true).SetId(this);
        _content.DOLocalRotate(Vector3.zero, _duration).SetEase(Ease.OutElastic, 1f, 0.5f).SetUpdate(true).SetId(this).OnComplete(EnableInteraction);
    }

    private void PlaySwingInHide()
    {
        _canvasGroup.DOFade(0f, _duration * 0.5f).SetUpdate(true).SetId(this);
        _content.DOAnchorPos(_originalAnchoredPos + new Vector2(0, Screen.height * 0.3f), _duration * 0.7f).SetEase(Ease.InBack).SetUpdate(true).SetId(this);
        _content.DOLocalRotate(new Vector3(0f, 0f, 25f), _duration * 0.7f).SetEase(Ease.InCubic).SetUpdate(true).SetId(this).OnComplete(Deactivate);
    }

    #endregion
    // -------------------------------------------------------
    #region Helpers

    private void EnableInteraction()
    {
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private void Deactivate() => gameObject.SetActive(false);

    #endregion
}