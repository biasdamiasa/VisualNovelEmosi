using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

namespace Emotionalaw
{
    public sealed class NovelFlow : MonoBehaviour
    {
        [Serializable]
        private struct ClueSlot
        {
            public string variable;
            [TextArea] public string description;
            public Text label;
        }

        [Header("Story")]
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private NovelPresenter presenter;
        [SerializeField] private ClueSlot[] clues;
        [Header("Authored screens")]
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject howToScreen;
        [SerializeField] private GameObject novelScreen;
        [SerializeField] private GameObject endScreen;
        [Header("HUD Display")]
        [Tooltip("Show the CLUES 0 / 6 counter in the top bar.")]
        [SerializeField] private bool showClueCounter = true;
        [Tooltip("Show the current chapter label, for example 01 / FEAR.")]
        [SerializeField] private bool showChapterLabel = true;
        [Tooltip("Show the What Helps Next panel on the Ending Screen.")]
        [SerializeField] private bool showEndingGuidance = true;
        [SerializeField] private GameObject clueCounter;
        [SerializeField] private GameObject chapterLabel;
        [SerializeField] private Text clueCountText;
        [SerializeField] private Text chapterText;
        [SerializeField] private NotesClueIndicator notesClueIndicator;
        [Header("Clue and interpretation popup")]
        [SerializeField] private GameObject popup;
        [SerializeField] private Text popupTitle;
        [SerializeField] private Text popupBody;
        [SerializeField] private Button popupContinue;
        [Header("Notebook / final review")]
        [SerializeField] private GameObject notebook;
        [SerializeField] private Text notebookSubtitle;
        [SerializeField] private Text notebookCloseLabel;
        [SerializeField] private Button notebookClose;
        [Header("Ending")]
        [SerializeField] private Text endingKind;
        [SerializeField] private Text endingTitle;
        [SerializeField] private Text endingSupport;
        [SerializeField] private Text endingStats;
        [SerializeField] private Text endingGuidance;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button startButton;

        private bool popupDismissed;
        private bool reviewDismissed;
        private bool starting;
        private GameObject priorSelection;
        public bool IsModalOpen => popup.activeSelf || notebook.activeSelf;
        public bool IsAtEnding => endScreen.activeSelf;
        public DialogueRunner Runner => runner;
        public Button PopupContinue => popupContinue;
        public Button NotebookClose => notebookClose;
        public bool PopupOpen => popup.activeSelf;
        public bool NotebookOpen => notebook.activeSelf;
        public bool ShowClueCounter => showClueCounter;
        public bool ShowChapterLabel => showChapterLabel;
        public bool ShowEndingGuidance => showEndingGuidance;
        public bool HasUnreadClue => notesClueIndicator.HasUnreadClue;

        private void Awake()
        {
            ApplyHudVisibility();
            runner.AddCommandHandler<string>("clue", RevealClue);
            runner.AddCommandHandler<string>("feedback", ShowFeedback);
            runner.AddCommandHandler("review", ReviewClues);
            runner.AddCommandHandler<string, string>("question", presenter.SetQuestion);
            runner.AddCommandHandler<string>("chapter", SetChapter);
            runner.AddCommandHandler<string>("ending", ShowEnding);
            runner.AddCommandHandler("intro_begin", presenter.BeginCharacterIntroductions);
            runner.AddCommandHandler<string, string>("introduce", presenter.IntroduceCharacter);
            runner.AddCommandHandler("conversation_begin", presenter.BeginGroupConversation);
            runner.AddCommandHandler<string, string>("join", presenter.AddCharacterToConversation);
            popupContinue.onClick.AddListener(DismissPopup);
            notebookClose.onClick.AddListener(CloseNotebook);
        }

        private void Start()
        {
            popup.SetActive(false);
            notebook.SetActive(false);
            ShowScreen(startScreen);
            startButton.Select();
        }

        private void OnValidate() => ApplyHudVisibility();

        private void ApplyHudVisibility()
        {
            if (clueCounter != null) clueCounter.SetActive(showClueCounter);
            if (chapterLabel != null) chapterLabel.SetActive(showChapterLabel);
            if (endingGuidance != null) endingGuidance.gameObject.SetActive(showEndingGuidance);
        }

        private void OnDestroy()
        {
            popupDismissed = reviewDismissed = true;
            popupContinue.onClick.RemoveListener(DismissPopup);
            notebookClose.onClick.RemoveListener(CloseNotebook);
            foreach (string command in new[] { "clue", "feedback", "review", "question", "chapter", "ending", "intro_begin", "introduce", "conversation_begin", "join" })
                runner.RemoveCommandHandler(command);
        }

        public bool ReadBool(string variable) => runner.VariableStorage.TryGetValue(variable, out bool value) && value;
        public string ReadString(string variable) => runner.VariableStorage.TryGetValue(variable, out string value) ? value : "";
        public int ClueCount
        {
            get
            {
                int count = 0;
                foreach (var clue in clues) if (ReadBool(clue.variable)) count++;
                return count;
            }
        }

        private void ShowScreen(GameObject selected)
        {
            startScreen.SetActive(selected == startScreen);
            howToScreen.SetActive(selected == howToScreen);
            novelScreen.SetActive(selected == novelScreen);
            endScreen.SetActive(selected == endScreen);
        }

        public void ShowHowTo() => ShowScreen(howToScreen);
        public void BeginGame() => BeginAsync().Forget();
        public void PlayAgain() => BeginAsync().Forget();

        private async YarnTask BeginAsync()
        {
            if (starting) return;
            starting = true;
            try
            {
                popupDismissed = reviewDismissed = true;
                if (runner.IsDialogueRunning) await runner.Stop();
                runner.VariableStorage.Clear();
                presenter.ResetPresentation();
                popup.SetActive(false);
                notebook.SetActive(false);
                clueCountText.text = "CLUES  0 / 6";
                notesClueIndicator.ResetIndicator();
                ShowScreen(novelScreen);
                await runner.StartDialogue("opening");
            }
            finally { starting = false; }
        }

        private void SetChapter(string title) => chapterText.text = title;

        private async YarnTask RevealClue(string key)
        {
            foreach (var clue in clues)
            {
                if (clue.variable != "$" + key) continue;
                clueCountText.text = "CLUES  " + ClueCount + " / 6";
                notesClueIndicator.NotifyClueFound();
                await ShowPopup("PETUNJUK TERBUKA", clue.description);
                return;
            }
            throw new ArgumentException("Unknown clue: " + key);
        }

        private YarnTask ShowFeedback(string emotion)
        {
            bool correct = ReadBool("$" + emotion + "Known");
            return ShowPopup(correct ? "EMOSI TERIDENTIFIKASI" : "INTERPRETASI TERCATAT",
                correct ? emotion.ToUpperInvariant() : "Kesimpulanmu telah dicatat. Cerita berlanjut.");
        }

        private async YarnTask ShowPopup(string title, string body)
        {
            popupTitle.text = title;
            popupBody.text = body;
            popupDismissed = false;
            popup.SetActive(true);
            popupContinue.Select();
            while (!popupDismissed && !destroyCancellationToken.IsCancellationRequested) await YarnTask.Yield();
            if (this != null) popup.SetActive(false);
        }

        private void DismissPopup() => popupDismissed = true;

        private void RefreshNotebook()
        {
            foreach (var clue in clues)
                clue.label.text = ReadBool(clue.variable) ? clue.description : "?  Unknown clue";
        }

        public void OpenNotebook()
        {
            if (IsModalOpen || !novelScreen.activeSelf) return;
            priorSelection = EventSystem.current.currentSelectedGameObject;
            RefreshNotebook();
            notesClueIndicator.MarkRead();
            notebookSubtitle.text = "Hanya petunjuk yang kamu temukan yang ditampilkan.";
            notebookCloseLabel.text = "KEMBALI KE PERCAKAPAN";
            notebook.SetActive(true);
            notebookClose.Select();
        }

        public void CloseNotebook()
        {
            notebook.SetActive(false);
            reviewDismissed = true;
            if (priorSelection != null && priorSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(priorSelection);
            priorSelection = null;
        }

        private async YarnTask ReviewClues()
        {
            RefreshNotebook();
            notebookSubtitle.text = "Periksa kembali petunjuk yang kamu punya sebelum menarik kesimpulan.";
            notebookCloseLabel.text = "LANJUT KE KESIMPULAN FINAL";
            reviewDismissed = false;
            notebook.SetActive(true);
            notebookClose.Select();
            while (!reviewDismissed && !destroyCancellationToken.IsCancellationRequested) await YarnTask.Yield();
        }

        private void ShowEnding(string result)
        {
            bool good = result == "good";
            endingKind.text = good ? "GOOD ENDING" : "BAD ENDING";
            endingTitle.text = good ? "DIPAHAMI" : "SALAH DIPAHAMI";
            endingSupport.text = good ? "Kamu memahami apa yang Rey tidak mampu ungkapkan secara langsung."
                : "Kamu melihat reaksinya, tetapi tidak menangkap apa yang ada di baliknya.";
            endingGuidance.text = good
                ? "WHAT HELPS NEXT\n\n• Dengarkan tanpa buru-buru memberi solusi.\n\n• Validasi: ‘Kedengarannya ini berat buat lu.’\n\n• Tanyakan: ‘Lu ingin didengar atau dibantu mencari langkah?’\n\n• Jika ini terus mengganggu keseharian, dukung ia mencari bantuan profesional."
                : "COBA PENDEKATAN LAIN\n\n• Hindari menyimpulkan perasaan seseorang terlalu cepat.\n\n• Ajukan pertanyaan terbuka dan dengarkan jawabannya.\n\n• Validasi perasaannya sebelum menawarkan solusi.\n\n• Ajak bicara lagi ketika ia sudah siap.";
            endingStats.text = "Clues found                       " + ClueCount + " / 6\n\n"
                + "Takut                              " + (ReadBool("$fearKnown") ? "Teridentifikasi" : "Terlewat") + "\n\n"
                + "Sedih                              " + (ReadBool("$sadnessKnown") ? "Teridentifikasi" : "Terlewat") + "\n\n"
                + "Marah                              " + (ReadBool("$angerKnown") ? "Teridentifikasi" : "Terlewat") + "\n\n"
                + "Kesimpulan Emosi                   " + ReadString("$finalAnswer");
            ShowScreen(endScreen);
            replayButton.Select();
        }
    }
}
