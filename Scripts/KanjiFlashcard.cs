using System;
using System.Collections.Generic;
using TMPro;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Karet.Prefabs
{
    public enum KanjiSet
    {
        Numbers,
        People,
        Body,
        Places,
        Vehicles,
        Time,
        Colors,
        Directions,
        DaysOfTheWeek,
        Nature,
        Animals,
        Languages,
        Things,
        Weather,
        Adjectives,
        Verbs
    }
    
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KanjiFlashcard : UdonSharpBehaviour
    {
        [SerializeField] private KanjiSet kanjiSet;
        
        [SerializeField] private GameObject selector;
        [SerializeField] private Transform cursor;
        [SerializeField] private GameObject desktopTooltip;
        
        private bool optionsOpen = false;
        private bool isDesktop = false; // set in start based on whether we detect a VR headset, affects input handling
        private int kanjiIndex = 0; // index of the currently displayed kanji in the selected set
        private VRC_Pickup pickupcomp;
        private bool selectionCoolDown = false; // prevent rapid input from moving the cursor multiple times in one button press
        
        [SerializeField] private TextMeshProUGUI characterText;
        [SerializeField] private TextMeshProUGUI romanjiText;
        
        //ui and backgrounds
        [SerializeField] private Image backgroundImage1;
        [SerializeField] private Image backgroundImage2;
        [SerializeField] Image optionPernounciationImage;
        [SerializeField] TextMeshProUGUI[] kanjiListText;
        
        private bool showPernounciation = true;
        private Color optionEnabledColor = new Color(0.3f, 8f, 0.4f);
        private Color kanjiColor = new Color(.3f, .8f, .6f);
        
        public readonly string[][] KanjiGroups = new string[][]
        {
            // 数字 (Numbers)
            new string[] { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "百", "千", "万" },

            // 人 (People)
            new string[] { "人", "日本人", "子ども", "男", "女", "男の子", "女の子", "父", "お父さん", "母", "お母さん", "先生", "友達", "高校生" },

            // 体 (Body)
            new string[] { "目", "口", "耳", "手", "足", "力" },

            // 場所 (Places)
            new string[] { "学校", "会社", "駅", "店", "空港", "大学", "入口", "出口" },

            // 乗り物 (Vehicles)
            new string[] { "車", "電車" },

            // 時間 (Time)
            new string[] { "時間", "一週間", "毎日", "毎週", "毎年", "今日", "明日", "今週", "先週", "来週", "今月", "先月", "来月", "新年", "午前", "午後" },

            // 色 (Colors)
            new string[] { "白", "黒" },

            // 方角 (Directions)
            new string[] { "上", "下", "右", "左", "前", "後", "中", "外", "北", "南", "東", "西" },

            // 曜日 (Days of the week)
            new string[] { "日曜日", "月曜日", "火曜日", "水曜日", "木曜日", "金曜日", "土曜日" },

            // 自然 (Nature)
            new string[] { "山", "川", "空", "空気", "花" },

            // 動物 (Animals)
            new string[] { "牛", "馬", "魚", "貝" },

            // 言語 (Languages)
            new string[] { "日本語", "中国語", "英語" },

            // 物 (Things)
            new string[] { "新聞", "本" },

            // 天気 (Weather)
            new string[] { "天気", "雨" },

            // 形容詞 (Adjectives)
            new string[] { "大きい", "小さい", "新しい", "古い", "高い", "安い", "多い", "少ない", "少し", "長い", "早い" },

            // 動詞 (Verbs)
            new string[] { "行く", "来る", "食べる", "飲む", "書く", "見る", "言う", "立つ", "出る", "入る", "話す", "読む", "買う", "聞く", "休む", "会う", "上がる" }
        };
        
        public readonly string[][] Pronunciations = new string[][]
        {
            // 数字 (Numbers)
            new string[] { "いち", "に", "さん", "よん / し", "ご", "ろく", "なな / しち", "はち", "きゅう / く", "じゅう", "ひゃく", "せん", "まん" },

            // 人 (People)
            new string[] { "ひと", "にほんじん", "こども", "おとこ", "おんな", "おとこのこ", "おんなのこ", "ちち", "おとうさん", "はは", "おかあさん", "せんせい", "ともだち", "こうこうせい" },

            // 体 (Body)
            new string[] { "め", "くち", "みみ", "て", "あし", "ちから" },

            // 場所 (Places)
            new string[] { "がっこう", "かいしゃ", "えき", "みせ", "くうこう", "だいがく", "いりぐち", "でぐち" },

            // 乗り物 (Vehicles)
            new string[] { "くるま", "でんしゃ" },

            // 時間 (Time)
            new string[] { "じかん", "いっしゅうかん", "まいにち", "まいしゅう", "まいとし", "きょう", "あした", "こんしゅう", "せんしゅう", "らいしゅう", "こんげつ", "せんげつ", "らいげつ", "しんねん", "ごぜん", "ごご" },

            // 色 (Colors)
            new string[] { "しろ", "くろ" },

            // 方角 (Directions)
            new string[] { "うえ", "した", "みぎ", "ひだり", "まえ", "うしろ", "なか", "そと", "きた", "みなみ", "ひがし", "にし" },

            // 曜日 (Days of the week)
            new string[] { "にちようび", "げつようび", "かようび", "すいようび", "もくようび", "きんようび", "どようび" },

            // 自然 (Nature)
            new string[] { "やま", "かわ", "そら", "くうき", "はな" },

            // 動物 (Animals)
            new string[] { "うし", "うま", "さかな", "かい" },

            // 言語 (Languages)
            new string[] { "にほんご", "ちゅうごくご", "えいご" },

            // 物 (Things)
            new string[] { "しんぶん", "ほん" },

            // 天気 (Weather)
            new string[] { "てんき", "あめ" },

            // 形容詞 (Adjectives)
            new string[] { "おおきい", "ちいさい", "あたらしい", "ふるい", "たかい", "やすい", "おおい", "すくない", "すこし", "ながい", "はやい" },

            // 動詞 (Verbs)
            new string[] { "いく", "くる", "たべる", "のむ", "かく", "みる", "いう", "たつ", "でる", "はいる", "はなす", "よむ", "かう", "きく", "やすむ", "あう", "あがる" }
        };


        public readonly string[][] Meanings = new string[][]
        {
            // 数字 (Numbers)
            new string[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "hundred", "thousand", "ten thousand" },

            // 人 (People)
            new string[] { "person", "Japanese", "child", "man", "woman", "boy", "girl", "(my) father", "father", "(my) mother", "mother", "teacher", "friend", "high school student" },

            // 体 (Body)
            new string[] { "eye", "mouth", "ear", "hand", "foot", "power" },

            // 場所 (Places)
            new string[] { "school", "company", "station", "shop", "airport", "university", "entrance", "exit" },

            // 乗り物 (Vehicles)
            new string[] { "car", "train" },

            // 時間 (Time)
            new string[] { "time", "one week", "every day", "every week", "every year", "today", "tomorrow", "this week", "last week", "next week", "this month", "last month", "next month", "new year", "A.M.", "P.M." },

            // 色 (Colors)
            new string[] { "white", "black" },

            // 方角 (Directions)
            new string[] { "above", "under", "right", "left", "front", "behind", "inside", "outside", "north", "south", "east", "west" },

            // 曜日 (Days of the week)
            new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },

            // 自然 (Nature)
            new string[] { "mountain", "river", "sky", "air", "flower" },

            // 動物 (Animals)
            new string[] { "cow", "horse", "fish", "shell" },

            // 言語 (Languages)
            new string[] { "Japanese language", "Chinese language", "English" },

            // 物 (Things)
            new string[] { "newspaper", "book" },

            // 天気 (Weather)
            new string[] { "weather", "rain" },

            // 形容詞 (Adjectives)
            new string[] { "big", "small", "new", "old", "expensive / tall", "cheap", "many", "few", "a few / a little", "long", "early" },

            // 動詞 (Verbs)
            new string[] { "to go", "to come", "to eat", "to drink", "to write", "to watch / look", "to say", "to stand", "to go out", "to enter", "to talk", "to read", "to buy", "to listen", "to rest", "to meet", "to go up" }
        };

        private void Start()
        {
            isDesktop = !Networking.LocalPlayer.IsUserInVR();
            pickupcomp = GetComponent<VRC_Pickup>();
            cursor.localPosition = kanjiListText[0].transform.localPosition;
            if (!isDesktop) pickupcomp.AutoHold = VRC_Pickup.AutoHoldMode.No;
            
            int index = 0;
            int set = (int)kanjiSet;
            
            //set the options
            for (int i = 0; i < KanjiGroups[set].Length; i++)
            {
                if (i >= kanjiListText.Length) break;
                kanjiListText[index].text = KanjiGroups[set][i];
                index++;
            }
            
            //set the rest to blank
            for (int i = index; i < kanjiListText.Length; i++)
            {
                kanjiListText[index].text = "";
            }
            
            //color
            backgroundImage1.color = kanjiColor;
            backgroundImage2.color = kanjiColor * .8f;
            optionPernounciationImage.color = showPernounciation ? optionEnabledColor : backgroundImage2.color;
            
            SetCard();
        }
        
        private void Update()
        {
            if (!isDesktop) return;
            if (!optionsOpen) return;
            
            //desktop options because they cant click the buttons
            if (Input.GetKeyDown(KeyCode.Q)) TogglePernounciation();
            if (Input.GetKeyDown(KeyCode.W)) SetDirection(-1f, false); // up
            if (Input.GetKeyDown(KeyCode.S)) SetDirection(1f, false); // down
            if (Input.GetKeyDown(KeyCode.A)) SetDirection(-1f, true); // left
            if (Input.GetKeyDown(KeyCode.D)) SetDirection(1f, true); // right
        }
        
        public void TogglePernounciation()
        {
            showPernounciation = !showPernounciation;
            optionPernounciationImage.color = showPernounciation ? optionEnabledColor : backgroundImage2.color;
            SetCard();
        }

        private void SetCard()
        {
            int set = (int)kanjiSet;
            characterText.text = KanjiGroups[set][kanjiIndex];
            romanjiText.text = showPernounciation ? Pronunciations[set][kanjiIndex] + "\n<size=60%>" + Meanings[set][kanjiIndex] : "";
        }
        
        public override void OnPickupUseDown()
        {
            //swap ui
            selector.SetActive(true); //animate later
            optionPernounciationImage.gameObject.SetActive(true);
            romanjiText.gameObject.SetActive(false);
            if (isDesktop) desktopTooltip.SetActive(true);
            
            //lock player to prevent moving while capturing input
            Networking.LocalPlayer.Immobilize(true);
            optionsOpen = true;
        }

        public override void OnPickupUseUp()
        {
            //undo all dat
            selector.SetActive(false);
            optionPernounciationImage.gameObject.SetActive(false);
            romanjiText.gameObject.SetActive(true);
            desktopTooltip.SetActive(false);
            Networking.LocalPlayer.Immobilize(false);
            optionsOpen = false;
            
            //set the option
            SetCard();
        }

        public override void OnDrop()
        {
            if (optionsOpen) OnPickupUseUp();
        }
        
        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            if (!optionsOpen) return;
            if (isDesktop) return;
            if (args.handType != HandType.LEFT) return;
            SetDirection(value, true);
        }
        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            if (!optionsOpen) return;
            if (isDesktop) return;
            if (args.handType != HandType.LEFT) return;
            SetDirection(value, false);
        }
        
        private void SetDirection(float value, bool horizontal)
        {
            if (selectionCoolDown)
            {
                if (value < 0.2f || value > -0.2f) selectionCoolDown = false; // reset cooldown when stick is released
                return;
            }
            
            if (Math.Abs(value) < 0.8f) return; // deadzone
            
            byte direction = 0;
            if (horizontal)
            {
                if (value > 0.5f) direction = 3; // right
                else if (value < -0.5f) direction = 1; // left
            }
            else
            {
                if (value > 0.5f) direction = 4; // down
                else if (value < -0.5f) direction = 2; // up
            }
            
            //move cursor to the selected option based on direction
            int setLength = KanjiGroups[(int)kanjiSet].Length -1; // -1 because of 0 indexing
            setLength = Mathf.Clamp(setLength, 0, 14); // clamp to the number of options we have available
            switch (direction)
            {
                case 1: kanjiIndex = Mathf.Clamp(kanjiIndex - 1, 0, setLength); break;
                case 2: kanjiIndex = Mathf.Clamp(kanjiIndex - 3, 0, setLength); break;
                case 3: kanjiIndex = Mathf.Clamp(kanjiIndex + 1, 0, setLength); break;
                case 4: kanjiIndex = Mathf.Clamp(kanjiIndex + 3, 0, setLength); break;
            }
            cursor.localPosition = kanjiListText[kanjiIndex].transform.localPosition;
            if (!isDesktop) selectionCoolDown = true;
        }
        
        public void ChangeCard(KanjiSet newSet)
        {
            kanjiSet = newSet;
            SetCard();
        }
    }
    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(KanjiFlashcard))]
    public class KanjiFlashcardEditor : Editor
    {
        private KanjiFlashcard flashcard;
        private SerializedProperty kanjiSetProp;

        private void OnEnable()
        {
            flashcard = (KanjiFlashcard)target;
            kanjiSetProp = serializedObject.FindProperty("kanjiSet");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(kanjiSetProp);
            GUILayout.Space(2);
            
            string[] characters;
            characters = flashcard.KanjiGroups[kanjiSetProp.intValue];
            GUILayout.Label(string.Join(" ", characters));
            GUILayout.Space(5);
            
            if (GUILayout.Button("Create set of Kanji cards"))
            {
                CreateCardSet();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void CreateCardSet()
        {
            //store all of these transforms in a list so we can arrange them in a grid
            List<Transform> cardTransforms = new List<Transform>();
            cardTransforms.Add(flashcard.transform);
            
            //change this card to be first set
            flashcard.ChangeCard(KanjiSet.Numbers);
            flashcard.gameObject.name = $"Kanji - {Enum.GetName(typeof(KanjiSet), 0)}";
            
            for (int i = 1; i < Enum.GetValues(typeof(KanjiSet)).Length; i++)
            {
                GameObject newCard = Instantiate(flashcard.gameObject);
                newCard.name = $"Kanji - {Enum.GetName(typeof(KanjiSet), i)}";
                
                KanjiFlashcard cardScript = newCard.GetComponent<KanjiFlashcard>();
                cardScript.ChangeCard((KanjiSet)i);
                
                //mark dirty and try to save it
                EditorUtility.SetDirty(newCard);
                cardTransforms.Add(newCard.transform);
            }
            
            //arrange them in a 3x3 grid with the final card in the center of the bottom row
            int columns = 4;
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