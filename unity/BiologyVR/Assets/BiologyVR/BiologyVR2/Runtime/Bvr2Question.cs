using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>"Predict, then see" cards: a question and three holographic answers in front of the player.
    /// Point with the BioTool (or the screen centre) and pull the trigger.</summary>
    public sealed class Bvr2Question : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Audio audioDirector; public Bvr2Fx fx; public Bvr2Hud hud;
        public bool Showing { get; private set; }
        public int LastChoice { get; private set; } = -1;
        public int ForcedChoice = -1; // tests / autopilot

        RectTransform canvas; Image[] cards; Text[] texts; int hover = -1; Transform current;

        /// <summary>Removes a board left open by an interrupted coroutine (restart).</summary>
        public void Cancel() { cancelled = true; if (current) Destroy(current.gameObject); current = null; Showing = false; if (hud) hud.TopHidden = false; Unfocus(); }
        bool cancelled;
        static void Unfocus() { var blood = Bvr2Game.Instance ? Bvr2Game.Instance.blood : null; if (blood) blood.ClearFocus(); Bvr2Environment.Current?.SetFocus(false); }

        public IEnumerator Ask(string question, string[] options, int correct, string tagText = null)
        {
            Showing = true; LastChoice = -1; cancelled = false;
            if (!hud) hud = FindFirstObjectByType<Bvr2Hud>(); if (hud) hud.TopHidden = true;
            var head = rig.head.transform;
            Vector3 f = Vector3.ProjectOnPlane(head.forward, Vector3.up); if (f.sqrMagnitude < 0.01f) f = head.forward; f.Normalize();
            var root = new GameObject("Question").transform; root.SetParent(rig.transform, true); current = root;
            root.position = head.position + f * 1.7f + Vector3.up * -0.12f; root.rotation = Quaternion.LookRotation(f, Vector3.up);
            canvas = Bvr2Ui.Canvas("Question Canvas", root, new Vector2(1500, 700), 0.001f, 85);
            Bvr2Ui.Backdrop(canvas);
            var blood = Bvr2Game.Instance ? Bvr2Game.Instance.blood : null; if (blood) blood.SetFocus(root.position, 1.0f);
            Bvr2Environment.Current?.SetFocus(true);
            var q = Bvr2Ui.Rect(canvas, "Q", new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -210), new Vector2(-40, -20));
            Bvr2Ui.Panel(q, Bvr2Style.Panel, "Back");
            var tag = Bvr2Ui.Label(q, tagText ?? (correct < 0 ? "ВЫБОР" : "ПРЕДСКАЖИ"), 30, Bvr2Style.Knowledge, TextAnchor.UpperLeft, FontStyle.Bold); tag.rectTransform.offsetMin = new Vector2(30, 0); tag.rectTransform.offsetMax = new Vector2(-30, -16);
            var qt = Bvr2Ui.Label(q, question, 50, Bvr2Style.Text, TextAnchor.MiddleLeft, FontStyle.Bold); qt.rectTransform.offsetMin = new Vector2(30, 14); qt.rectTransform.offsetMax = new Vector2(-30, -56);
            Bvr2Ui.Fit(qt, 28, 50);
            cards = new Image[options.Length]; texts = new Text[options.Length];
            // up to four cards in a row; more (the chapter choice) go in two rows
            int rows = options.Length > 4 ? 2 : 1, cols = Mathf.CeilToInt(options.Length / (float)rows);
            float w = 1400f / cols, cardH = rows == 1 ? 330f : 158f;
            for (int i = 0; i < options.Length; i++)
            {
                var r = Bvr2Ui.Rect(canvas, "Card " + i, new Vector2(0, 0), new Vector2(0, 0));
                int row = i / cols, col = i % cols;
                r.pivot = new Vector2(0.5f, 0.5f); r.sizeDelta = new Vector2(w - 40, cardH); r.anchoredPosition = new Vector2(50 + w * (col + 0.5f), rows == 1 ? 230 : 315 - row * 172);
                cards[i] = Bvr2Ui.Panel(r, new Color(0.04f, 0.12f, 0.16f, 0.9f), "Back");
                var num = Bvr2Ui.Label(r, (i + 1).ToString(), 44, Bvr2Style.Ally, TextAnchor.UpperLeft, FontStyle.Bold); num.rectTransform.offsetMin = new Vector2(24, 0); num.rectTransform.offsetMax = new Vector2(0, -14);
                texts[i] = Bvr2Ui.Label(r, options[i], 44, Bvr2Style.Text, TextAnchor.MiddleCenter, FontStyle.Bold); texts[i].rectTransform.offsetMin = new Vector2(24, 24); texts[i].rectTransform.offsetMax = new Vector2(-24, -60);
                Bvr2Ui.Fit(texts[i], 26, 44);
            }
            var hint = Bvr2Ui.Rect(canvas, "Hint", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 56));
            Bvr2Ui.Label(hint, rig.IsXR ? "наведи BioTool и нажми курок" : $"наведи прицел и нажми ЛКМ  ·  или клавиши 1–{options.Length}", 30, Bvr2Style.TextDim, TextAnchor.MiddleCenter);
            audioDirector?.Play("UI_Panel", 0.8f);
            root.localScale = Vector3.one * 0.6f;
            int chosen = -1; float t = 0;
            while (chosen < 0)
            {
                if (cancelled || !root) yield break;
                t += Time.unscaledDeltaTime; root.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(t / 0.3f));
                int h = Hover();
                if (h != hover) { hover = h; if (h >= 0) { audioDirector?.Play("UI_Hover", 0.5f); Bvr2Haptics.Pulse(false, 0.15f, 0.03f); } }
                for (int i = 0; i < cards.Length; i++) cards[i].color = i == hover ? new Color(0.1f, 0.35f, 0.42f, 0.95f) : new Color(0.04f, 0.12f, 0.16f, 0.9f);
                if (input.ToolDown && hover >= 0 && t > 0.4f) chosen = hover;
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key, kb.digit7Key, kb.digit8Key };
                    for (int i = 0; i < keys.Length && i < options.Length; i++) if (keys[i].wasPressedThisFrame) chosen = i;
                }
                if (ForcedChoice >= 0) { chosen = Mathf.Clamp(ForcedChoice, 0, options.Length - 1); ForcedChoice = -1; }
                yield return null;
            }
            LastChoice = chosen;
            bool right = correct < 0 || chosen == correct;
            audioDirector?.Play(right ? "SFX_Success_Step" : "SFX_Error", 0.9f);
            Bvr2Haptics.Pulse(false, right ? 0.5f : 0.3f, 0.1f);
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].color = (i == correct || (correct < 0 && i == chosen)) ? new Color(0.1f, 0.45f, 0.22f, 0.95f) : i == chosen ? new Color(0.5f, 0.16f, 0.08f, 0.95f) : new Color(0.03f, 0.06f, 0.08f, 0.7f);
                if (i == correct) texts[i].text += "  ✓";
            }
            if (fx) fx.Burst(cards[chosen].transform.position, right ? Bvr2Style.Good : Bvr2Style.Danger, 26, 1.6f, 0.04f, 0.6f);
            float w2 = 0; while (w2 < 1.6f) { if (cancelled || !root) yield break; w2 += Time.unscaledDeltaTime; yield return null; }
            float k = 0; while (k < 0.3f) { if (cancelled || !root) yield break; k += Time.unscaledDeltaTime; root.localScale = Vector3.one * Mathf.Lerp(1f, 0.01f, k / 0.3f); yield return null; }
            Destroy(root.gameObject); Showing = false; if (hud) hud.TopHidden = false; Unfocus();
        }

        int Hover()
        {
            var hand = rig.IsXR ? rig.rightHand : rig.head.transform;
            var ray = new Ray(hand.position, hand.forward);
            for (int i = 0; i < cards.Length; i++)
            {
                var rt = cards[i].rectTransform; var plane = new Plane(-rt.forward, rt.position);
                if (!plane.Raycast(ray, out float d)) continue;
                Vector3 local = rt.InverseTransformPoint(ray.GetPoint(d));
                if (rt.rect.Contains(new Vector2(local.x, local.y))) return i;
            }
            return -1;
        }
    }
}
