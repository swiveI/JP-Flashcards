using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Karet.Prefabs
{
    public enum KanaType
    {
        Hiragana,
        Katakana
    }

    public enum ConsonantType
    {
        A,
        Ka,
        Sa,
        Ta,
        Na,
        Ha,
        Ma,
        Ya,
        Ra,
        Wa,
    }
    
    [UdonBehaviourSyncMode(BehaviourSyncMode.None), RequireComponent(typeof(VRC_Pickup))] //might sync later if people want
    public class JpFlashcard : UdonSharpBehaviour
    {
        [SerializeField] private KanaType flashCardType;
        [SerializeField] private ConsonantType consonantType;
        
        [SerializeField] private GameObject selector;
        [SerializeField] private Transform cursor;
        [SerializeField] private GameObject desktopTooltip;
        private bool optionsOpen = false;
        private bool isDesktop = false; // set in start based on whether we detect a VR headset, affects input handling
        private byte direction = 0; // 0-4 for center, left, up, right, down flicks respectively
        private Vector2 inputVector;
        private VRC_Pickup pickupcomp;
        
        [SerializeField] private TextMeshProUGUI characterText;
        [SerializeField] private TextMeshProUGUI romanjiText;
        [SerializeField] private TextMeshProUGUI direction0Text;
        [SerializeField] private TextMeshProUGUI direction1Text;
        [SerializeField] private TextMeshProUGUI direction2Text;
        [SerializeField] private TextMeshProUGUI direction3Text;
        [SerializeField] private TextMeshProUGUI direction4Text;
        
        //ui and backgrounds
        [SerializeField] private Image backgroundImage1;
        [SerializeField] private Image backgroundImage2;
        
        private bool showPernounciation = true;
        private bool showDakuten = false;
        private bool showHandakuten = false;
        private bool hasDakuten = false;
        private bool hasHandakuten = false;
        
        [SerializeField] Image optionPernounciationImage;
        [SerializeField] Image optionDakutenImage;
        [SerializeField] Image optionHandakutenImage;
        
        private Color optionEnabledColor = new Color(0.3f, 8f, 0.4f);
        private Color hiraganaColor = new Color(.3f, 0.6f, 1f);
        private Color katakanaColor = new Color(.8f, .3f, 0.4f);
        
        public readonly string[][][][] FlickKana = new string[][][][]
        {
            // Row 0: A
            new string[][][] {
                new string[][] { new string[] { "あ","い","う","え","お" }, null, null },
                new string[][] { new string[] { "ア","イ","ウ","エ","オ" }, null, null }
            },
            // Row 1: Ka
            new string[][][] {
                new string[][] { new string[] { "か","き","く","け","こ" }, new string[] { "が","ぎ","ぐ","げ","ご" }, null },
                new string[][] { new string[] { "カ","キ","ク","ケ","コ" }, new string[] { "ガ","ギ","グ","ゲ","ゴ" }, null }
            },
            // Row 2: Sa
            new string[][][] {
                new string[][] { new string[] { "さ","し","す","せ","そ" }, new string[] { "ざ","じ","ず","ぜ","ぞ" }, null },
                new string[][] { new string[] { "サ","シ","ス","セ","ソ" }, new string[] { "ザ","ジ","ズ","ゼ","ゾ" }, null }
            },
            // Row 3: Ta
            new string[][][] {
                new string[][] { new string[] { "た","ち","つ","て","と" }, new string[] { "だ","ぢ","づ","で","ど" }, null },
                new string[][] { new string[] { "タ","チ","ツ","テ","ト" }, new string[] { "ダ","ヂ","ヅ","デ","ド" }, null }
            },
            // Row 4: Na
            new string[][][] {
                new string[][] { new string[] { "な","に","ぬ","ね","の" }, null, null },
                new string[][] { new string[] { "ナ","ニ","ヌ","ネ","ノ" }, null, null }
            },
            // Row 5: Ha (has handakuten)
            new string[][][] {
                new string[][] { new string[] { "は","ひ","ふ","へ","ほ" }, new string[] { "ば","び","ぶ","べ","ぼ" }, new string[] { "ぱ","ぴ","ぷ","ぺ","ぽ" } },
                new string[][] { new string[] { "ハ","ヒ","フ","ヘ","ホ" }, new string[] { "バ","ビ","ブ","ベ","ボ" }, new string[] { "パ","ピ","プ","ペ","ポ" } }
            },
            // Row 6: Ma
            new string[][][] {
                new string[][] { new string[] { "ま","み","む","め","も" }, null, null },
                new string[][] { new string[] { "マ","ミ","ム","メ","モ" }, null, null }
            },
            // Row 7: Ya
            new string[][][] {
                new string[][] { new string[] { "や","（","ゆ","）","よ" }, null, null },
                new string[][] { new string[] { "ヤ","（","ユ","）","ヨ" }, null, null }
            },
            // Row 8: Ra
            new string[][][] {
                new string[][] { new string[] { "ら","り","る","れ","ろ" }, null, null },
                new string[][] { new string[] { "ラ","リ","ル","レ","ロ" }, null, null }
            },
            // Row 9: Wa
            new string[][][] {
                new string[][] { new string[] { "わ","を","ん","ー","〜" }, null, null },
                new string[][] { new string[] { "ワ","ヲ","ン","ー","〜" }, null, null }
            }
        };

        public readonly string[][][] FlickRomaji = new string[][][]
        {
            // Row 0: A
            new string[][] { new string[] { "a", "i", "u", "e", "o" }, null, null },
            // Row 1: Ka
            new string[][] { new string[] { "ka","ki","ku","ke","ko" }, new string[] { "ga","gi","gu","ge","go" }, null },
            // Row 2: Sa
            new string[][] { new string[] { "sa","shi","su","se","so" }, new string[] { "za","ji","zu","ze","zo" }, null },
            // Row 3: Ta
            new string[][] { new string[] { "ta","chi","tsu","te","to" }, new string[] { "da","ji","zu","de","do" }, null },
            // Row 4: Na
            new string[][] { new string[] { "na","ni","nu","ne","no" }, null, null },
            // Row 5: Ha
            new string[][] { new string[] { "ha","hi","fu","he","ho" }, new string[] { "ba","bi","bu","be","bo" }, new string[] { "pa","pi","pu","pe","po" } },
            // Row 6: Ma
            new string[][] { new string[] { "ma","mi","mu","me","mo" }, null, null },
            // Row 7: Ya
            new string[][] { new string[] { "ya","(", "yu",")","yo" }, null, null },
            // Row 8: Ra
            new string[][] { new string[] { "ra","ri","ru","re","ro" }, null, null },
            // Row 9: Wa
            new string[][] { new string[] { "wa","wo","n","-","~" }, null, null }
        };
        
        
        private void Start()
        {
            isDesktop = !Networking.LocalPlayer.IsUserInVR();
            pickupcomp = GetComponent<VRC_Pickup>();
            if (!isDesktop) pickupcomp.AutoHold = VRC_Pickup.AutoHoldMode.No;
            SetCard();
            
            //fill out the directions text
            direction0Text.text = GetFlashCardString(0);
            direction1Text.text = GetFlashCardString(1);
            direction2Text.text = GetFlashCardString(2);
            direction3Text.text = GetFlashCardString(3);
            direction4Text.text = GetFlashCardString(4);
            
            switch (flashCardType)
            {
                case KanaType.Hiragana: 
                    backgroundImage1.color = hiraganaColor;
                    backgroundImage2.color = hiraganaColor * .8f;
                    break;
                case KanaType.Katakana: 
                    backgroundImage1.color = katakanaColor; 
                    backgroundImage2.color = katakanaColor * .8f;
                    break;
            }
            
            //set button colors
            optionPernounciationImage.color = showPernounciation ? optionEnabledColor : backgroundImage2.color;
            optionDakutenImage.color = showDakuten ? optionEnabledColor : backgroundImage2.color;
                optionHandakutenImage.color = showHandakuten ? optionEnabledColor : backgroundImage2.color;
        }

        private void Update()
        {
            if (!isDesktop) return;
            if (!optionsOpen) return;
            
            //desktop options because they cant click the buttons
            if (Input.GetKeyDown(KeyCode.Q)) TogglePernounciation();
            if (Input.GetKeyDown(KeyCode.E)) ToggleDakuten();
            if (Input.GetKeyDown(KeyCode.G)) ToggleHandakuten();
        }

        public override void OnPickupUseDown()
        {
            //swap ui
            selector.SetActive(true); //animate later
            optionPernounciationImage.gameObject.SetActive(true);
            if (hasDakuten) optionDakutenImage.gameObject.SetActive(true);
            if (hasHandakuten) optionHandakutenImage.gameObject.SetActive(true);
            romanjiText.gameObject.SetActive(false);
            if (isDesktop) desktopTooltip.SetActive(true);
            
            //lock player to prevent moving while capturing input
            Networking.LocalPlayer.Immobilize(true);
            optionsOpen = true;
            inputVector = Vector2.zero;
        }

        public override void OnPickupUseUp()
        {
            //undo all dat
            selector.SetActive(false);
            optionPernounciationImage.gameObject.SetActive(false);
            optionDakutenImage.gameObject.SetActive(false);
            optionHandakutenImage.gameObject.SetActive(false);
            romanjiText.gameObject.SetActive(true);
            desktopTooltip.SetActive(false);
            Networking.LocalPlayer.Immobilize(false);
            optionsOpen = false;
            
            //move cursor back to center
            cursor.localPosition = Vector3.zero;
            inputVector = Vector2.zero;
            
            //set the option
            SetCard(direction);
            direction = 0;
        }

        public override void OnDrop()
        {
            if (optionsOpen) OnPickupUseUp();
        }
        
        //input events, gonna be kinda hard but need to diffrentiate for desktop vs vr. desktop only uses the move event but vr is hand specific
        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            if (!optionsOpen) return;
            if (args.handType != HandType.LEFT) return;
            SetDirection(value, true);
        }
        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            if (!optionsOpen) return;
            if (args.handType != HandType.LEFT) return;
            SetDirection(value, false);
        }
        public override void InputLookHorizontal(float value, UdonInputEventArgs args)
        {
            if (!optionsOpen) return;
            if (isDesktop) return;
            if (args.handType != HandType.RIGHT) return;
            SetDirection(value, true);
        }
        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            if (!optionsOpen) return;
            if (isDesktop) return;
            if (args.handType != HandType.RIGHT) return;
            SetDirection(value, false);
        }

        private void SetDirection(float value, bool horizontal)
        {
            //add onto the direction vector but clamp 0-1
            if (horizontal)
                inputVector.x = Mathf.Clamp(inputVector.x + value, -1f, 1f);
            else
                inputVector.y = Mathf.Clamp(inputVector.y + value, -1f, 1f);
            
            //set direction based on which axis has the highest absolute value, with a deadzone of 0.5
            if (inputVector.magnitude < 0.5f)
                direction = 0; // center
            else if (Mathf.Abs(inputVector.x) > Mathf.Abs(inputVector.y))
                direction = (byte)(inputVector.x > 0 ? 3 : 1); // right : left
            else
                direction = (byte)(inputVector.y > 0 ? 2 : 4); // up : down
            
            //move cursor to the selected option based on direction
            Vector3 pos = Vector3.zero;
            switch (direction)
            {
                case 1: pos = direction1Text.transform.localPosition; break;
                case 2: pos = direction2Text.transform.localPosition; break;
                case 3: pos = direction3Text.transform.localPosition; break;
                case 4: pos = direction4Text.transform.localPosition; break;
                default: pos = direction0Text.transform.localPosition; break;
            }
            cursor.localPosition = pos;
        }

        public void SetCard(byte flickdirection = 0)
        {
            characterText.text = GetFlashCardString(flickdirection);

            romanjiText.text = showPernounciation ? GetFlashCardString(flickdirection, true) : "";
            
            hasDakuten = (FlickKana[(int)consonantType][0][1] != null);
            hasHandakuten = (FlickKana[(int)consonantType][0][2] != null);
        }
        
        public void TogglePernounciation()
        {
            showPernounciation = !showPernounciation;
            optionPernounciationImage.color = showPernounciation ? optionEnabledColor : backgroundImage2.color;
            SetCard();
        }
        
        public void ToggleDakuten()
        {
            if (!hasDakuten) return;
            showDakuten = !showDakuten;
            optionDakutenImage.color = showDakuten ? optionEnabledColor : backgroundImage2.color;
            
            //toggle off other one
            showHandakuten = false;
            optionHandakutenImage.color = backgroundImage2.color;
            
            
            SetCard();
        }

        public void ToggleHandakuten()
        {
            if (!hasHandakuten) return;
            showHandakuten = !showHandakuten;
            optionHandakutenImage.color = showHandakuten ? optionEnabledColor : backgroundImage2.color;
            
            showDakuten = false;
            optionDakutenImage.color = backgroundImage2.color;
            
            SetCard();
        }
        
        private string GetFlashCardString(byte direction, bool Romanji = false)
        {
            int row = (int)consonantType;
            if (row < 0 || row > 9 || direction > 4) return "?";

            // If pronunciation is requested, return romaji
            if (Romanji)
            {
                int variantRo = 0;
                if (showDakuten && FlickRomaji[row][1] != null) variantRo = 1;
                if (showHandakuten && FlickRomaji[row][2] != null) variantRo = 2;

                if (FlickRomaji[row][variantRo] != null) return FlickRomaji[row][variantRo][direction];

                return FlickRomaji[row][0][direction]; // fallback
            }

            // Otherwise return kana (Hiragana or Katakana)
            int script = (flashCardType == KanaType.Hiragana) ? 0 : 1;

            int variant = 0;
            if (showDakuten && FlickKana[row][script][1] != null) variant = 1;
            if (showHandakuten && FlickKana[row][script][2] != null) variant = 2;

            if (FlickKana[row][script][variant] != null)
                return FlickKana[row][script][variant][direction];

            return FlickKana[row][script][0][direction]; // fallback
        }
        
        public void ChangeCard(KanaType kanaType, int consonant)
        {
            flashCardType = kanaType;
            consonantType = (ConsonantType)consonant;
            SetCard();
        }
    }
    
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    
    [UnityEditor.CustomEditor(typeof(JpFlashcard))]
    public class JpFlashcardEditor : UnityEditor.Editor
    {
        private JpFlashcard flashcardTarget;
        private SerializedProperty kanaTypeProp;
        private SerializedProperty consonantTypeProp;
        private SerializedProperty kanjiTypeProp;
        
        private void OnEnable()
        {
            flashcardTarget = (JpFlashcard)target;
            kanaTypeProp = serializedObject.FindProperty("flashCardType");
            consonantTypeProp = serializedObject.FindProperty("consonantType");
            kanjiTypeProp = serializedObject.FindProperty("kanjiType");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            //dropdown options
            KanaType kanaType = (KanaType)kanaTypeProp.intValue;
            EditorGUILayout.PropertyField(kanaTypeProp);
            EditorGUILayout.PropertyField(consonantTypeProp);
            GUILayout.Space(2);
            
            //draw all charecters in that row
            string[] characters;
            characters = flashcardTarget.FlickKana[consonantTypeProp.intValue][(kanaType == KanaType.Hiragana) ? 0 : 1][0];
            GUILayout.Label(string.Join(" ", characters));
            GUILayout.Space(5);
            
            string kanaTypeName = Enum.GetName(typeof(KanaType), kanaTypeProp.intValue);
            GUILayout.Space(5);
            if (GUILayout.Button($"Create set of {kanaTypeName} cards"))
            {
                CreateKanaCards(kanaType);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void CreateKanaCards(KanaType kanaType)
        {
            string kanaTypeName = Enum.GetName(typeof(KanaType), kanaTypeProp.intValue);
            
            //store all of these transforms in a list so we can arrange them in a grid
            List<Transform> cardTransforms = new List<Transform>();
            cardTransforms.Add(flashcardTarget.transform);
            
            //change this card to be the first consonant type of the selected kana type
            flashcardTarget.ChangeCard(kanaType, 0);
            flashcardTarget.gameObject.name = $"{kanaTypeName} - {Enum.GetName(typeof(ConsonantType), 0)}";
            
            //using this as a prefab create one for every consonant type
            for (int i = 1; i < Enum.GetValues(typeof(ConsonantType)).Length; i++)
            {
                GameObject newCard = Instantiate(flashcardTarget.gameObject);
                newCard.name = $"{kanaTypeName} - {Enum.GetName(typeof(ConsonantType), i)}";
                
                JpFlashcard cardScript = newCard.GetComponent<JpFlashcard>();
                cardScript.ChangeCard(kanaType, i);
                
                //mark dirty and try to save it
                EditorUtility.SetDirty(newCard);
                cardTransforms.Add(newCard.transform);
            }
            
            //arrange them in a 3x3 grid with the final card in the center of the bottom row
            int columns = 3;
            int rows = Mathf.CeilToInt(cardTransforms.Count / (float)columns);
            float spacing = 0.3f; // adjust as needed
            for (int i = 0; i < cardTransforms.Count; i++)
            {
                int row = i / columns;
                int column = i % columns;
                if (i == cardTransforms.Count - 1) column = 1; // place the final card in the center of the bottom row
                float xOffset = (column - (columns - 1) / 2f) * spacing;
                float yOffset = ((rows - 1) / 2f - row) * spacing;
                cardTransforms[i].localPosition = new Vector3(xOffset, yOffset, 0);
            }
        }
    }
    #endif
}