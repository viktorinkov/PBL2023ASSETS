using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MatchingGameTemplate.Types;
using System.Collections.Generic;

namespace MatchingGameTemplate
{
    /// <summary>
    /// This script controls the game, starting it, following game progress, and finishing it with game over.
    /// </summary>
    public class MGTGameController : MonoBehaviour
    {
        // Ad Unit ID 
        public InterstitialAd adLoader;


        // Holds the current event system
        internal EventSystem eventSystem;

        // Defines the type of pairs we have. These are either "ImageText" or "TextText" 
        internal string pairsType = "";

        [Tooltip("A list of all the pairs in the game. The text is derived from the image name.")]
        [HideInInspector]
        public Sprite[] pairs = null;

        [Tooltip("A list of all the pairs in the game. These pairs are for Text+Text. If an MGTPairsTextText component is attached to this gamecontroller, it will override the default pairs array.")]
        [HideInInspector]
        public PairTextText[] pairsTextText = null;

        [Tooltip("A list of all the pairs in the game. These pairs are for Text+Text. If an MGTPairsTextText component is attached to this gamecontroller, it will override the default pairs array.")]
        [HideInInspector]
        public PairTextTextMulti[] pairsTextTextMulti = null;

        [Tooltip("A list of all the pairs in the game. These pairs are for sound+image. If an MGTPairsImageSound component is attached to this gamecontroller, it will override the default pairs array.")]
        [HideInInspector]
        public PairImageSound[] pairsImageSound = null;

        [Tooltip("A list of all the pairs in the game. These pairs are for image+image. If an MGTPairsImageImage component is attached to this gamecontroller, it will override the default pairs array.")]
        [HideInInspector]
        public PairImageImage[] pairsImageImage = null;

        [Tooltip("The default player ball button which all other player balls are duplicated from and displayed in the shop grid")]
        public RectTransform defaultButton;

        [Tooltip("The group that holds the first part of each pair")]
        public RectTransform firstGroup;

        [Tooltip("The group that holds the second part of each pair")]
        public RectTransform secondGroup;

        [Tooltip("Randomize the list of pairs so that we don't get the same pairs each time we start the game")]
        public bool randomizePairs = true;

        [Tooltip("How many points we get for each matched pair. This value is multiplied by the number of the level we are on. Ex: Level 1 give 100 points, Level 2 gives 200 points.")]
        public float bonusPerLevel;

        // The current level we are on, 1 being the first level in the game
        internal int currentLevel = 1;

        // The text object that shows which level we are in. This text object should be placed inside the GameController object and named "LevelText"
        internal Text levelText;

        [Tooltip("The name of a numbered level ( ex: Round 1, Level 1, etc")]
        public string levelNamePrefix = "Level";

        // An array that holds all the pairs of the current level. This is used so that we can later remove them easily when the level is completed.
        internal Transform[] pairsArray;

        internal List<Transform> pairsWrongList = new List<Transform>();

        // The index of the current pair we are on. This increases as we advance in the levels
        internal int currentPair = 0;

        // The number of pairs left in the current level
        internal int pairsLeft = 0;

        [Tooltip("The number of pairs in the first level")]
        public int pairsCount = 4;

        [Tooltip("The number of pairs added to the game in each level")]
        public int pairsIncrease = 2;

        [Tooltip("The maximum number of pairs allowed in the game")]
        public int pairsMaximum = 8;

        // Did we select the first object in the pair, or the second?
        internal bool firstOfPair = true;

        // Holds the first object of the pair that the player selects.
        internal Transform firstObject;

        //The current score of the player
        internal float score = 0;
        internal float scoreCount = 0;

        // The score text object which displays the current score of the player. This text object should be placed inside the GameController object and named "LevelScore"
        internal Text scoreText;

        // The highest score we got in this game
        internal float highScore = 0;

        [Tooltip("How many seconds are left before game is over")]
        public float time = 10;
        internal float timeLeft;
        public float timeBonus = 2;

        //The canvas of the timer in the game, the UI object and its various parts
        internal GameObject timerIcon;
        internal Image timerBar;
        internal Text timerText;

        // Holds the cursor object which shows us where we are aiming when using a keyboard or gamepad
        internal RectTransform cursor;

        [Tooltip("The menu that appears when we pause the game")]
        public Transform pauseCanvas;

        [Tooltip("The menu that appears if we lose the game, when time runs out")]
        public Transform gameOverCanvas;

        [Tooltip("The menu that appears if we win the game, when finishing all the pairs")]
        public Transform victoryCanvas;

        // Is the game over?
        internal bool isGameOver = false;

        [Tooltip("The level of the main menu that can be loaded after the game ends")]
        public string mainMenuLevelName = "CS_StartMenu";

        [Tooltip("Various sounds and their source")]
        public AudioClip soundSelect;
        public AudioClip soundCorrect;
        public AudioClip soundWrong;
        public AudioClip soundTimeUp;
        public AudioClip soundLevelUp;
        public AudioClip soundGameOver;
        public AudioClip soundVictory;
        public string soundSourceTag = "Sound";
        internal GameObject soundSource;

        // The button that will restart the game after game over
        public string confirmButton = "Submit";

        // The button that pauses the game. Clicking on the pause button in the UI also pauses the game
        public string pauseButton = "Cancel";
        internal bool isPaused = false;
        internal GameObject buttonBeforePause;

        // A general use index
        internal int index = 0;

        /// <summary>
        /// Start is only called once in the lifetime of the behaviour.
        /// The difference between Awake and Start is that Start is only called if the script instance is enabled.
        /// This allows you to delay any initialization code, until it is really needed.
        /// Awake is always called before any Start functions.
        /// This allows you to order initialization of scripts
        /// </summary>
        void Start()
        {
            // Disable multitouch so that we don't tap two answers at the same time ( prevents multi-answer cheating, thanks to Miguel Paolino for catching this bug )
            Input.multiTouchEnabled = false;

            // Cache the current event system so we can position the cursor correctly
            eventSystem = UnityEngine.EventSystems.EventSystem.current;

            //Hide the game over ,victory ,and larger image screens
            if (gameOverCanvas) gameOverCanvas.gameObject.SetActive(false);
            if (victoryCanvas) victoryCanvas.gameObject.SetActive(false);
            if (pauseCanvas) pauseCanvas.gameObject.SetActive(false);

            // Assign the score text object
            if (GameObject.Find("ScoreText")) scoreText = GameObject.Find("ScoreText").GetComponent<Text>();

            //Update the score
            UpdateScore();

            // Assign the level text object
            if (GameObject.Find("LevelText")) levelText = GameObject.Find("LevelText").GetComponent<Text>();

            //Get the highscore for the player
            highScore = PlayerPrefs.GetFloat(SceneManager.GetActiveScene().name + "HighScore", 0);

            //Assign the timer icon and text for quicker access
            if (GameObject.Find("TimerIcon"))
            {
                timerIcon = GameObject.Find("TimerIcon");
                if (GameObject.Find("TimerIcon/Bar")) timerBar = GameObject.Find("TimerIcon/Bar").GetComponent<Image>();
                if (GameObject.Find("TimerIcon/Text")) timerText = GameObject.Find("TimerIcon/Text").GetComponent<Text>();
            }

            // Assign the cursor object and hide it, but only for non-mobile devices
            if (GameObject.Find("Cursor"))
            {
                cursor = GameObject.Find("Cursor").GetComponent<RectTransform>();

                cursor.gameObject.SetActive(false);
            }

            // Set the maximum value of the timer
            timeLeft = time;

            //Assign the sound source for easier access
            if (GameObject.FindGameObjectWithTag(soundSourceTag)) soundSource = GameObject.FindGameObjectWithTag(soundSourceTag);

            // Shuffle all the pairs in the game. If we have a TextText array, shuffle it instead.
            if (pairs.Length > 0)
            {
                // Set the pairs type
                pairsType = "ImageText";

                // Randomize the list of pairs
                if (randomizePairs == true) pairs = ShuffleSpritePairs(pairs);

                // Create the first level, Image - Text game mode
                UpdateLevel();
            }
            else if (pairsTextText.Length > 0)
            {
                // Set the pairs type
                pairsType = "TextText";

                // Randomize the list of pairs
                if (randomizePairs == true) pairsTextText = ShuffleTextTextPairs(pairsTextText);

                // Create the first level, Text - Text game mode
                UpdateLevelTextText();
            }
            else if (pairsImageSound.Length > 0)
            {
                // Set the pairs type
                pairsType = "ImageSound";

                // Randomize the list of pairs
                if (randomizePairs == true) pairsImageSound = ShuffleImageSoundPairs(pairsImageSound);

                // Create the first level, Text - Text game mode
                UpdateLevelImageSound();
            }
            else if (pairsImageImage.Length > 0)
            {
                // Set the pairs type
                pairsType = "ImageImage";

                // Randomize the list of pairs
                if (randomizePairs == true) pairsImageImage = ShuffleImageImagePairs(pairsImageImage);

                // Create the first level, Text - Text game mode
                UpdateLevelImageImage();
            }
            else if (pairsTextTextMulti.Length > 0)
            {
                // Set the pairs type
                pairsType = "TextTextMulti";

                // Randomize the list of pairs
                if (randomizePairs == true) pairsTextTextMulti = ShuffleTextTextMultiPairs(pairsTextTextMulti);

                // Create the first level, Text - Text game mode
                UpdateLevelTextTextMulti();
            }
        }

        /// <summary>
		/// Update is called every frame, if the MonoBehaviour is enabled.
		/// </summary>
		void Update()
        {
            // Make the score count up to its current value
            if (scoreCount < score)
            {
                // Count up to the courrent value
                scoreCount = Mathf.Lerp(scoreCount, score, Time.deltaTime * 10);

                // Update the score text
                UpdateScore();
            }

            //If the game is over, listen for the Restart and MainMenu buttons
            if (isGameOver == true)
            {
                //The jump button restarts the game
                if (Input.GetButtonDown(confirmButton))
                {
                    Restart();
                }

                //The pause button goes to the main menu
                if (Input.GetButtonDown(pauseButton))
                {
                    MainMenu();
                }
            }
            else
            {
                if (cursor)
                {
                    // If we use the keyboard or gamepad, keyboardControls take effect
                    if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0) cursor.gameObject.SetActive(true);

                    // If we move the mouse in any direction or click it, or touch the screen on a mobile device, then keyboard/gamepad controls are lost
                    if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0 || Input.GetMouseButtonDown(0) || Input.touchCount > 0) cursor.gameObject.SetActive(false);

                    // Place the cursor at the position of the selected object
                    if (eventSystem.currentSelectedGameObject) cursor.position = eventSystem.currentSelectedGameObject.GetComponent<RectTransform>().position;
                }

                // Count down the time until game over
                if (timeLeft > 0)
                {
                    // Count down the time
                    timeLeft -= Time.deltaTime;
                }

                // Update the timer
                UpdateTime();

                //Toggle pause/unpause in the game
                if (Input.GetButtonDown(pauseButton))
                {
                    if (isPaused == true) Unpause();
                    else Pause(true);
                }
            }
        }

        /// <summary>
        /// Pause the game, and shows the pause menu
        /// </summary>
        /// <param name="showMenu">If set to <c>true</c> show menu.</param>
        public void Pause(bool showMenu)
        {
            isPaused = true;

            //Set timescale to 0, preventing anything from moving
            Time.timeScale = 0;

            // Remember the button that was selected before pausing
            if (eventSystem) buttonBeforePause = eventSystem.currentSelectedGameObject;

            //Show the pause screen and hide the game screen
            if (showMenu == true)
            {
                if (pauseCanvas) pauseCanvas.gameObject.SetActive(true);

                // Hide the game screen
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void Unpause()
        {
            isPaused = false;

            //Set timescale back to the current game speed
            Time.timeScale = 1;

            //Hide the pause screen and show the game screen
            if (pauseCanvas) pauseCanvas.gameObject.SetActive(false);

            // Show the game screen
            gameObject.SetActive(true);

            // Select the button that we pressed before pausing
            if (eventSystem) eventSystem.SetSelectedGameObject(buttonBeforePause);
        }

        /// <summary>
        /// Selects an object for the pair. If two objects are selected, they are compared to see if they match
        /// </summary>
        /// <param name="selectedObject"></param>
        public void SelectObject(Transform selectedObject)
        {
            if (firstOfPair == true) // Selecting the first object of a pair
            {
                // Animate the object when selected
                if (selectedObject.GetComponent<Animator>()) selectedObject.GetComponent<Animator>().Play("PairSelect");

                firstObject = selectedObject;

                firstOfPair = false;

                //If there is a source and a sound, play it from the source
                if (soundSource)
                {
                    // Play the select sound
                    if (soundSelect) soundSource.GetComponent<AudioSource>().PlayOneShot(soundSelect);

                    // If this button has a sound assingned to it, play it
                    if (selectedObject.GetComponent<MGTPlaySound>()) selectedObject.GetComponent<MGTPlaySound>().PlaySoundCurrent();
                }

                // If we are playing a Sound-image game mode, make the image buttons interactable again, so we can select one of them
                if (pairsType == "ImageSound")
                {
                    foreach (RectTransform imageButton in firstGroup)
                    {
                        if ( imageButton.GetComponent<MGTButton>().isActive == true ) imageButton.GetComponent<Button>().interactable = true;
                    }
                }
            }
            else // Selecting the second object of a pair
            {
                if (firstObject == selectedObject) //If we click on the same object that we already selected, unselect it and set it back to idle state ( in animation )
                {
                    if (selectedObject.GetComponent<Animator>())
                    {
                        firstObject.GetComponent<Animator>().Play("PairIdle");
                        selectedObject.GetComponent<Animator>().Play("PairIdle");
                    }
                }
                // If we selected two objects, check if they match. In Image - Text mode we compare the image of the object to the name of the other object. In Text - Text mode we compare the pairs and see if they have the same firstText and secondText values
                //else if (pairsType == "ImageText" && (firstObject.Find("Image").GetComponent<Image>().sprite.name == selectedObject.Find("Text").GetComponent<Text>().text || selectedObject.Find("Image").GetComponent<Image>().sprite.name == firstObject.Find("Text").GetComponent<Text>().text) || pairsType == "TextText" && (firstObject.Find("Text2").GetComponent<Text>().text == selectedObject.Find("Text2").GetComponent<Text>().text || selectedObject.Find("Text2").GetComponent<Text>().text == firstObject.Find("Text2").GetComponent<Text>().text) || pairsType == "ImageSound" && (firstObject.Find("Image").GetComponent<Image>().sprite.name == selectedObject.Find("Image").GetComponent<Image>().sprite.name || selectedObject.Find("Image").GetComponent<Image>().sprite.name == firstObject.Find("Image").GetComponent<Image>().sprite.name) || pairsType == "ImageImage" && (firstObject.Find("Image").GetComponent<Image>().sprite.name == selectedObject.Find("Image").GetComponent<Image>().sprite.name))
                else if (firstObject.name == selectedObject.name && firstObject.name != "XWrongAnswerX" && selectedObject.name != "XWrongAnswerX" )
                {
                    // Play the correct animation
                    if (selectedObject.GetComponent<Animator>())
                    {
                        firstObject.GetComponent<Animator>().Play("PairCorrect");
                        selectedObject.GetComponent<Animator>().Play("PairCorrect");

                        firstObject.GetComponent<MGTButton>().isActive = false;
                        selectedObject.GetComponent<MGTButton>().isActive = false;
                    }

                    // Add the bonus to the score
                    ChangeScore(bonusPerLevel * currentLevel);

                    // Make the buttons uninteractable, so we don't have to move through them in the grid
                    firstObject.GetComponent<Button>().interactable = false;
                    selectedObject.GetComponent<Button>().interactable = false;

                    // Stop listening for a click on these objects
                    firstObject.GetComponent<Button>().onClick.RemoveAllListeners();// RemoveListener(delegate { SelectObject(firstObject); });
                    selectedObject.GetComponent<Button>().onClick.RemoveAllListeners();// RemoveListener(delegate { SelectObject(selectedObject); });

                    //If there is a source and a sound, play it from the source
                    if (soundSource && soundCorrect) soundSource.GetComponent<AudioSource>().PlayOneShot(soundCorrect);

                    // One less pair to match
                    pairsLeft--;

                    // If there are no more pairs to match, move to the next level
                    if (pairsLeft <= 0) StartCoroutine("LevelUp", 0.5f);
                }
                else // If there is no match between the two objects
                {
                    // Play the wrong animation
                    if (selectedObject.GetComponent<Animator>())
                    {
                        firstObject.GetComponent<Animator>().Play("PairWrong");
                        selectedObject.GetComponent<Animator>().Play("PairWrong");
                    }

                    //If there is a source and a sound, play it from the source
                    if (soundSource && soundWrong) soundSource.GetComponent<AudioSource>().PlayOneShot(soundWrong);
                }

                // Reset the pair check
                firstOfPair = true;

                // If we are playing a Sound-image game mode, make the image buttons unclikcable
                if (pairsType == "ImageSound")
                {
                    foreach (RectTransform imageButton in firstGroup) imageButton.GetComponent<Button>().interactable = false;
                }

                // Select one of the clickable buttons in the second group
                if (eventSystem)
                {
                    foreach (RectTransform imageButton in secondGroup)
                    {
                        if (imageButton.GetComponent<Button>().interactable == true) eventSystem.SetSelectedGameObject(imageButton.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Shuffles the specified sprite pairs list, and returns it
        /// </summary>
        /// <param name="pairs">A list of sprite pairs</param>
        Sprite[] ShuffleSpritePairs(Sprite[] pairs)
        {
            // Go through all the sprite pairs and shuffle them
            for (index = 0; index < pairs.Length; index++)
            {
                // Hold the pair in a temporary variable
                Sprite tempPair = pairs[index];

                // Choose a random index from the sprite pairs list
                int randomIndex = UnityEngine.Random.Range(index, pairs.Length);

                // Assign a random sprite pair from the list
                pairs[index] = pairs[randomIndex];

                // Assign the temporary sprite pair to the random question we chose
                pairs[randomIndex] = tempPair;
            }

            return pairs;
        }

        /// <summary>
        /// Shuffles the specified text strings list, and returns it
        /// </summary>
        /// <param name="pairs">A list of text strings</param>
        PairTextText[] ShuffleTextTextPairs(PairTextText[] pairs)
        {
            // Go through all the text strings and shuffle them
            for (index = 0; index < pairs.Length; index++)
            {
                // Hold the text string in a temporary variable
                PairTextText tempPair = pairs[index];

                // Choose a random index from the text strings list
                int randomIndex = UnityEngine.Random.Range(index, pairs.Length);

                // Assign a random text string from the list
                pairs[index] = pairs[randomIndex];

                // Assign the temporary text string to the random question we chose
                pairs[randomIndex] = tempPair;
            }

            return pairs;
        }

        /// <summary>
        /// Shuffles the specified text strings list, and returns it
        /// </summary>
        /// <param name="pairs">A list of text strings</param>
        PairImageSound[] ShuffleImageSoundPairs(PairImageSound[] pairs)
        {
            // Go through all the text strings and shuffle them
            for (index = 0; index < pairs.Length; index++)
            {
                // Hold the text string in a temporary variable
                PairImageSound tempPair = pairs[index];

                // Choose a random index from the text strings list
                int randomIndex = UnityEngine.Random.Range(index, pairs.Length);

                // Assign a random text string from the list
                pairs[index] = pairs[randomIndex];

                // Assign the temporary text string to the random question we chose
                pairs[randomIndex] = tempPair;
            }

            return pairs;
        }

        /// <summary>
        /// Shuffles the specified text strings list, and returns it
        /// </summary>
        /// <param name="pairs">An array of image+image pairs</param>
        PairImageImage[] ShuffleImageImagePairs(PairImageImage[] pairs)
        {
            // Go through all the text strings and shuffle them
            for (index = 0; index < pairs.Length; index++)
            {
                // Hold the text string in a temporary variable
                PairImageImage tempPair = pairs[index];

                // Choose a random index from the text strings list
                int randomIndex = UnityEngine.Random.Range(index, pairs.Length);

                // Assign a random text string from the list
                pairs[index] = pairs[randomIndex];

                // Assign the temporary text string to the random question we chose
                pairs[randomIndex] = tempPair;
            }

            return pairs;
        }

        /// <summary>
        /// Shuffles the specified text strings list, and returns it
        /// </summary>
        /// <param name="pairs">A list of text strings</param>
        PairTextTextMulti[] ShuffleTextTextMultiPairs(PairTextTextMulti[] pairs)
        {
            // Go through all the text strings and shuffle them
            for (index = 0; index < pairs.Length; index++)
            {
                // Hold the text string in a temporary variable
                PairTextTextMulti tempPair = pairs[index];

                // Choose a random index from the text strings list
                int randomIndex = UnityEngine.Random.Range(index, pairs.Length);

                // Assign a random text string from the list
                pairs[index] = pairs[randomIndex];

                // Assign the temporary text string to the random question we chose
                pairs[randomIndex] = tempPair;
            }

            return pairs;
        }



        /// <summary>
        /// Shuffles the specified pairs list, and returns it
        /// </summary>
        /// <param name="pairs">A list of pairs</param>
        string[] ShuffleTexts(string[] pairs)
        {
            // Go through all the pairs and shuffle them
            for (index = 0; index < pairs.Length; index++)
            {
                // Hold the pair in a temporary variable
                string tempPair = pairs[index];

                // Choose a random index from the question list
                int randomIndex = UnityEngine.Random.Range(index, pairs.Length);

                // Assign a random question from the list
                pairs[index] = pairs[randomIndex];

                // Assign the temporary question to the random question we chose
                pairs[randomIndex] = tempPair;
            }

            return pairs;
        }

        /// <summary>
        /// Updates the timer text, and checks if time is up
        /// </summary>
        void UpdateTime()
        {
            // Update the time only if we have a timer icon canvas assigned
            if (timerIcon)
            {
                // Update the timer circle, if we have one
                if (timerBar)
                {
                    // If the timer is running, display the fill amount left. Otherwise refill the amount back to 100%
                    timerBar.fillAmount = timeLeft / time;
                }

                // Update the timer text, if we have one
                if (timerText)
                {
                    // If the timer is running, display the timer left. Otherwise hide the text
                    timerText.text = Mathf.RoundToInt(timeLeft).ToString();
                }

                // Time's up!
                if (timeLeft <= 0)
                {
                    // Run the game over event
                    StartCoroutine(GameOver(0.5f));

                    // Play the timer icon Animator
                    if (timerIcon.GetComponent<Animation>()) timerIcon.GetComponent<Animation>().Play();

                    //If there is a source and a sound, play it from the source
                    if (soundSource && soundTimeUp) soundSource.GetComponent<AudioSource>().PlayOneShot(soundTimeUp);
                }
            }
        }

        /// <summary>
		/// Change the score
		/// </summary>
		/// <param name="changeValue">Change value</param>
		void ChangeScore(float changeValue)
        {
            score += changeValue;

            //Update the score
            UpdateScore();
        }

        /// <summary>
        /// Updates the score value and checks if we got to the next level
        /// </summary>
        void UpdateScore()
        {
            //Update the score text
            if (scoreText) scoreText.text = Mathf.CeilToInt(scoreCount).ToString();
        }

        /// <summary>
		/// Levels up, and increases the difficulty of the game
		/// </summary>
		IEnumerator LevelUp(float delay)
        {
            yield return new WaitForSeconds(delay);

            // Clear all the current pairs
            foreach (Transform pair in pairsArray)
            {
                if (pair) pair.gameObject.SetActive(false);
            }

            pairsArray = new Transform[0];

            // Clear all wrong answers in TextTextMulti mode
            foreach (Transform pairWrong in pairsWrongList)
            {
                if (pairWrong) Destroy(pairWrong.gameObject);
            }

            pairsWrongList.Clear();

            // Show the next set of pairs from the list
            if (currentPair < pairs.Length || currentPair < pairsTextText.Length || currentPair < pairsTextTextMulti.Length || currentPair < pairsImageSound.Length || currentPair < pairsImageImage.Length)
            {
                currentLevel++;

                // Increase the number of pairs in the level
                pairsCount += pairsIncrease;

                // Limit the number of pairs to the maximum value
                pairsCount = Mathf.Clamp(pairsCount, 0, pairsMaximum);

                //If there is a source and a sound, play it from the source
                if (soundSource && soundLevelUp) soundSource.GetComponent<AudioSource>().PlayOneShot(soundLevelUp);

                // Update the level attributes based on the type of pairs we have
                if (pairsType == "ImageText") UpdateLevel();
                else if (pairsType == "TextText") UpdateLevelTextText();
                else if (pairsType == "TextTextMulti") UpdateLevelTextTextMulti();
                else if (pairsType == "ImageSound") UpdateLevelImageSound();
                else if (pairsType == "ImageImage") UpdateLevelImageImage();

                // Add time bonus
                timeLeft += timeBonus;

                UpdateTime();
            }
            else // Otherwise if we finished all pairs in the game, go to the Victory screen
            {
                // Run the victory event
                StartCoroutine(Victory(0.5f));
            }
        }

        /// <summary>
        /// Updates the level, showing the next set of pairs ( Used for Image - Text matching )
        /// </summary>
        void UpdateLevel()
        {
            // Display the current level we are on
            if (levelText) levelText.text = levelNamePrefix + " " + (currentLevel).ToString();

            // If we have a default button assigned and we have a list of pairs, duplicate the button and display all the pairs in the grid
            if (defaultButton && pairs.Length > 0)
            {
                // Deactivate the default button as we don't need it anymore
                defaultButton.gameObject.SetActive(false);

                // The number of pairs we need to create for this level, which 
                int levelPairs = currentPair + pairsCount;

                // Set the length of the array that holds the pairs in this level
                pairsArray = new Transform[pairsCount * 2];

                int arrayIndex = 0;

                // Create the shop buttons by duplicating the default one
                for (index = currentPair; index < pairs.Length; index++)
                {
                    // Create a new button
                    RectTransform newButton = Instantiate(defaultButton) as RectTransform;

                    // Listen for a click to change to the next stage
                    newButton.GetComponent<Button>().onClick.AddListener(delegate { SelectObject(newButton); });

                    if (firstOfPair == true)
                    {
                        if (firstGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(firstGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        // Show the image and hide the text
                        newButton.GetComponent<Image>().enabled = true;
                        newButton.Find("Text").GetComponent<Text>().enabled = false;

                        // Set the other text object to a generic string, because we don't use it in this game mode ( only in Text-Text mode )
                        newButton.Find("Text2").GetComponent<Text>().text = "Text1";

                        // Assign the image from the pair
                        newButton.Find("Image").GetComponent<Image>().sprite = pairs[index];

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        firstOfPair = false;

                        index--;
                    }
                    else
                    {
                        if (secondGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(secondGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        // Hide the image and show the text
                        newButton.Find("Image").GetComponent<Image>().enabled = false;
                        newButton.Find("Text").GetComponent<Text>().enabled = true;

                        // Assign the text from the pair
                        newButton.Find("Text").GetComponent<Text>().text = pairs[index].name;

                        // Set the other text object to a generic string, because we don't use it in this game mode ( only in Text-Text mode )
                        newButton.Find("Text2").GetComponent<Text>().text = "Text2";

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        pairsLeft++;

                        currentPair++;

                        firstOfPair = true;
                    }

                    // Deactivate the button until we actually need it
                    newButton.gameObject.SetActive(true);

                    // Select the object for keyboard/gamepad controls
                    if (eventSystem) eventSystem.SetSelectedGameObject(newButton.gameObject);

                    // Add the button to an array, so that we can shuffle the pairs, and later remove them when the level is completed
                    pairsArray[arrayIndex] = newButton;

                    arrayIndex++;

                    // If we passed through all the pairs in the game, stop creating them
                    if (currentPair >= levelPairs) break;
                }

                // Reset the pairs array index
                arrayIndex = 0;

                // Reset the pair check. The next time we click on an object it will be the first of a pair
                firstOfPair = true;

                // We will do several randomization passes on the pairs array, shuffling them
                int randomCount = pairsArray.Length;

                // Repeat the shuffle several times
                while (randomCount > 0)
                {
                    if (randomCount < pairsArray.Length)
                    {
                        int randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose one of the elements in the array randomly and put it at the start
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsFirstSibling();

                        randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose another of the elements in the array randomly and put it at the end
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsLastSibling();
                    }

                    randomCount--;
                }
            }
        }

        /// <summary>
        /// Updates the level, showing the next set of pairs ( Used for Text - Text matching )
        /// </summary>
        void UpdateLevelTextText()
        {
            // Display the current level we are on
            if (levelText) levelText.text = levelNamePrefix + " " + (currentLevel).ToString();

            // If we have a default button assigned and we have a list of pairs, duplicate the button and display all the pairs in the grid
            if (defaultButton && pairsTextText.Length > 0)
            {
                // Deactivate the default button as we don't need it anymore
                defaultButton.gameObject.SetActive(false);

                // The number of pairs we need to create for this level, which 
                int levelPairs = currentPair + pairsCount;

                // Set the length of the array that holds the pairs in this level
                pairsArray = new Transform[pairsCount * 2];

                int arrayIndex = 0;

                // Create the shop buttons by duplicating the default one
                for (index = currentPair; index < pairsTextText.Length; index++)
                {
                    // Create a new button
                    RectTransform newButton = Instantiate(defaultButton) as RectTransform;

                    // Listen for a click to change to the next stage
                    newButton.GetComponent<Button>().onClick.AddListener(delegate { SelectObject(newButton); });

                    if (firstOfPair == true)
                    {
                        if (firstGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(firstGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        // Hide the image and show the text
                        newButton.Find("Image").GetComponent<Image>().enabled = false;
                        newButton.Find("Text").GetComponent<Text>().enabled = true;

                        // Set the texts for both parts of the pair, so that when we compare them we can find which texts match
                        newButton.Find("Text").GetComponent<Text>().text = pairsTextText[index].firstText;
                        newButton.Find("Text2").GetComponent<Text>().text = pairsTextText[index].firstText;

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        firstOfPair = false;

                        index--;
                    }
                    else
                    {
                        if (secondGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(secondGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        // Hide the image and show the text
                        newButton.Find("Image").GetComponent<Image>().enabled = false;
                        newButton.Find("Text").GetComponent<Text>().enabled = true;

                        // Set the texts for both parts of the pair, so that when we compare them we can find which texts match
                        newButton.Find("Text").GetComponent<Text>().text = pairsTextText[index].secondText;
                        newButton.Find("Text2").GetComponent<Text>().text = pairsTextText[index].firstText;

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        pairsLeft++;

                        currentPair++;

                        firstOfPair = true;
                    }

                    // Deactivate the button until we actually need it
                    newButton.gameObject.SetActive(true);

                    // Select the object for keyboard/gamepad controls
                    if (eventSystem) eventSystem.SetSelectedGameObject(newButton.gameObject);

                    // Add the button to an array, so that we can shuffle the pairs, and later remove them when the level is completed
                    pairsArray[arrayIndex] = newButton;

                    arrayIndex++;

                    // If we passed through all the pairs in the game, stop creating them
                    if (currentPair >= levelPairs) break;
                }

                // Reset the pairs array index
                arrayIndex = 0;

                // Reset the pair check. The next time we click on an object it will be the first of a pair
                firstOfPair = true;

                // We will do several randomization passes on the pairs array, shuffling them
                int randomCount = pairsArray.Length;

                // Repeat the shuffle several times
                while (randomCount > 0)
                {
                    if (randomCount < pairsArray.Length)
                    {
                        int randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose one of the elements in the array randomly and put it at the start
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsFirstSibling();

                        randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose another of the elements in the array randomly and put it at the end
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsLastSibling();
                    }

                    randomCount--;
                }
            }
        }

        void UpdateLevelTextTextMulti()
        {
            // Display the current level we are on
            if (levelText) levelText.text = levelNamePrefix + " " + (currentLevel).ToString();

            // If we have a default button assigned and we have a list of pairs, duplicate the button and display all the pairs in the grid
            if (defaultButton && pairsTextTextMulti.Length > 0)
            {
                // Deactivate the default button as we don't need it anymore
                defaultButton.gameObject.SetActive(false);

                // The number of pairs we need to create for this level, which 
                int levelPairs = currentPair + pairsCount;

                // Set the length of the array that holds the pairs in this level
                pairsArray = new Transform[pairsCount * 2];

                int arrayIndex = 0;

                // Create the shop buttons by duplicating the default one
                for (index = currentPair; index < pairsTextTextMulti.Length; index++)
                {
                    // Create a new button
                    RectTransform newButton = Instantiate(defaultButton) as RectTransform;

                    // Listen for a click to change to the next stage
                    newButton.GetComponent<Button>().onClick.AddListener(delegate { SelectObject(newButton); });

                    if (firstOfPair == true)
                    {
                        if (firstGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(firstGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        // Hide the image and show the text
                        newButton.Find("Image").GetComponent<Image>().enabled = false;
                        newButton.Find("Text").GetComponent<Text>().enabled = true;

                        // Set the texts for both parts of the pair, so that when we compare them we can find which texts match
                        newButton.Find("Text").GetComponent<Text>().text = pairsTextTextMulti[index].firstText;
                        newButton.Find("Text2").GetComponent<Text>().text = pairsTextTextMulti[index].firstText;

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        firstOfPair = false;

                        index--;
                    }
                    else
                    {
                        if (secondGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(secondGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        // Hide the image and show the text
                        newButton.Find("Image").GetComponent<Image>().enabled = false;
                        newButton.Find("Text").GetComponent<Text>().enabled = true;

                        // Set the texts for both parts of the pair, so that when we compare them we can find which texts match
                        newButton.Find("Text").GetComponent<Text>().text = pairsTextTextMulti[index].multiText[pairsTextTextMulti[index].correctAnswer];
                        newButton.Find("Text2").GetComponent<Text>().text = pairsTextTextMulti[index].firstText;

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        pairsLeft++;

                        currentPair++;

                        firstOfPair = true;

                        // Add false answers
                        for ( int wrongAnswerIndex = 0; wrongAnswerIndex < pairsTextTextMulti[index].multiText.Length; wrongAnswerIndex++ )
                        {
                            if ( pairsTextTextMulti[index].correctAnswer != wrongAnswerIndex )
                            {
                                RectTransform newButtonWrong = Instantiate(defaultButton) as RectTransform;

                                // Listen for a click to change to the next stage
                                newButtonWrong.GetComponent<Button>().onClick.AddListener(delegate { SelectObject(newButtonWrong); });

                                if (secondGroup)
                                {
                                    // Put it inside the button grid
                                    newButtonWrong.transform.SetParent(secondGroup);

                                    // Set the scale to the default button's scale
                                    newButtonWrong.localScale = defaultButton.localScale;

                                    // Set the position to the default button's position
                                    newButtonWrong.position = defaultButton.position;
                                }

                                // Hide the image and show the text
                                newButtonWrong.Find("Image").GetComponent<Image>().enabled = false;
                                newButtonWrong.Find("Text").GetComponent<Text>().enabled = true;

                                // Set the texts for both parts of the pair, so that when we compare them we can find which texts match
                                newButtonWrong.Find("Text").GetComponent<Text>().text = pairsTextTextMulti[index].multiText[wrongAnswerIndex];
                                newButtonWrong.Find("Text2").GetComponent<Text>().text = pairsTextTextMulti[index].firstText;

                                // Set the name of the matching pair
                                newButtonWrong.name = "XWrongAnswerX";

                                newButtonWrong.gameObject.SetActive(true);

                                pairsWrongList.Add(newButtonWrong);
                            }

                        }
                    }

                    // Go through all the wrong answers, and shuffle them
                    // We will do several randomization passes on the pairs array, shuffling them
                    int randomCountWrong = pairsWrongList.Count;

                    // Repeat the shuffle several times
                    while (randomCountWrong > 0)
                    {
                        if (randomCountWrong < pairsWrongList.Count)
                        {
                            int randomIndex = Mathf.FloorToInt(Random.Range(0, pairsWrongList.Count));

                            // Choose one of the elements in the array randomly and put it at the start
                            if (pairsWrongList[randomIndex]) pairsWrongList[randomIndex].SetAsFirstSibling();

                            randomIndex = Mathf.FloorToInt(Random.Range(0, pairsWrongList.Count));

                            // Choose another of the elements in the array randomly and put it at the end
                            if (pairsWrongList[randomIndex]) pairsWrongList[randomIndex].SetAsLastSibling();
                        }

                        randomCountWrong--;
                    }

                    // Deactivate the button until we actually need it
                    newButton.gameObject.SetActive(true);

                    // Select the object for keyboard/gamepad controls
                    if (eventSystem) eventSystem.SetSelectedGameObject(newButton.gameObject);

                    // Add the button to an array, so that we can shuffle the pairs, and later remove them when the level is completed
                    pairsArray[arrayIndex] = newButton;

                    arrayIndex++;

                    // If we passed through all the pairs in the game, stop creating them
                    if (currentPair >= levelPairs) break;
                }

                // Reset the pairs array index
                arrayIndex = 0;

                // Reset the pair check. The next time we click on an object it will be the first of a pair
                firstOfPair = true;

                // We will do several randomization passes on the pairs array, shuffling them
                int randomCount = pairsArray.Length;

                // Repeat the shuffle several times
                while (randomCount > 0)
                {
                    if (randomCount < pairsArray.Length)
                    {
                        int randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose one of the elements in the array randomly and put it at the start
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsFirstSibling();

                        randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose another of the elements in the array randomly and put it at the end
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsLastSibling();
                    }

                    randomCount--;
                }
            }
        }

        /// <summary>
        /// Updates the level, showing the next set of pairs ( Used for Image - Sound matching )
        /// </summary>
        void UpdateLevelImageSound()
        {
            // Display the current level we are on
            if (levelText) levelText.text = levelNamePrefix + " " + (currentLevel).ToString();

            // If we have a default button assigned and we have a list of pairs, duplicate the button and display all the pairs in the grid
            if (defaultButton && pairsImageSound.Length > 0)
            {
                // Deactivate the default button as we don't need it anymore
                defaultButton.gameObject.SetActive(false);

                // The number of pairs we need to create for this level, which 
                int levelPairs = currentPair + pairsCount;

                // Set the length of the array that holds the pairs in this level
                pairsArray = new Transform[pairsCount * 2];

                int arrayIndex = 0;

                // Create the shop buttons by duplicating the default one
                for (index = currentPair; index < pairsImageSound.Length; index++)
                {
                    // Create a new button
                    RectTransform newButton = Instantiate(defaultButton) as RectTransform;

                    // Listen for a click to select the button
                    newButton.GetComponent<Button>().onClick.AddListener(delegate { SelectObject(newButton); });

                    if (firstOfPair == true)
                    {
                        if (firstGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(firstGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        // Hide the image
                        newButton.Find("Text").GetComponent<Text>().enabled = false;

                        newButton.Find("Image").GetComponent<Image>().enabled = true;
                        newButton.Find("Image").GetComponent<Image>().sprite = pairsImageSound[index].image;

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        if (newButton.Find("SoundIcon")) newButton.Find("SoundIcon").GetComponent<Image>().enabled = false;

                        newButton.GetComponent<Button>().interactable = false;

                        //newButton.GetComponent<MGTPlaySound>().sound = pairsImageSound[index].sound;

                        // Set the sound for both parts of the pair, so that when we compare them we can find which texts match
                        //newButton.Find("Text2").GetComponent<Text>().text = newButton.Find("Image").GetComponent<Image>().name;
                        //newButton.Find("Text2").GetComponent<Text>().text = newButton.Find("Image").GetComponent<Image>().sprite.name;

                        firstOfPair = false;

                        index--;
                    }
                    else
                    {
                        if (secondGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(secondGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        newButton.Find("Image").GetComponent<Image>().enabled = true;


                        // Hide the image and show the text
                        newButton.Find("Text").GetComponent<Text>().enabled = false;

                        newButton.Find("Image").GetComponent<Image>().enabled = false;
                        newButton.Find("Image").GetComponent<Image>().sprite = pairsImageSound[index].image;

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        newButton.GetComponent<MGTPlaySound>().sound = pairsImageSound[index].sound;

                        //newButton.Find("Image").GetComponent<Image>().sprite = pairsImageSound[index].image;

                        // Set the sound for both parts of the pair, so that when we compare them we can find which texts match
                        //newButton.Find("Text").GetComponent<Text>().text = newButton.Find("Image").GetComponent<Image>().name;
                        //newButton.Find("Text2").GetComponent<Text>().text = newButton.Find("Image").GetComponent<Image>().sprite.name;

                        pairsLeft++;

                        currentPair++;

                        firstOfPair = true;
                    }

                    // Deactivate the button until we actually need it
                    newButton.gameObject.SetActive(true);

                    // Select the object for keyboard/gamepad controls
                    if (eventSystem) eventSystem.SetSelectedGameObject(newButton.gameObject);

                    // Add the button to an array, so that we can shuffle the pairs, and later remove them when the level is completed
                    pairsArray[arrayIndex] = newButton;

                    arrayIndex++;

                    // If we passed through all the pairs in the game, stop creating them
                    if (currentPair >= levelPairs) break;
                }

                // Reset the pairs array index
                arrayIndex = 0;

                // Reset the pair check. The next time we click on an object it will be the first of a pair
                firstOfPair = true;

                // We will do several randomization passes on the pairs array, shuffling them
                int randomCount = pairsArray.Length;

                // Repeat the shuffle several times
                while (randomCount > 0)
                {
                    if (randomCount < pairsArray.Length)
                    {
                        int randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose one of the elements in the array randomly and put it at the start
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsFirstSibling();

                        randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose another of the elements in the array randomly and put it at the end
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsLastSibling();
                    }

                    randomCount--;
                }
            }
        }

        /// <summary>
        /// Updates the level, showing the next set of pairs ( Used for Image - Image matching )
        /// </summary>
        void UpdateLevelImageImage()
        {
            // Display the current level we are on
            if (levelText) levelText.text = levelNamePrefix + " " + (currentLevel).ToString();

            // If we have a default button assigned and we have a list of pairs, duplicate the button and display all the pairs in the grid
            if (defaultButton && pairsImageImage.Length > 0)
            {
                // Deactivate the default button as we don't need it anymore
                defaultButton.gameObject.SetActive(false);

                // The number of pairs we need to create for this level, which 
                int levelPairs = currentPair + pairsCount;

                // Set the length of the array that holds the pairs in this level
                pairsArray = new Transform[pairsCount * 2];

                int arrayIndex = 0;

                // Create the shop buttons by duplicating the default one
                for (index = currentPair; index < pairsImageImage.Length; index++)
                {
                    // Create a new button
                    RectTransform newButton = Instantiate(defaultButton) as RectTransform;

                    // Listen for a click to select the button
                    newButton.GetComponent<Button>().onClick.AddListener(delegate { SelectObject(newButton); });

                    if (firstOfPair == true)
                    {
                        if (firstGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(firstGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        // Hide the image
                        newButton.Find("Text").GetComponent<Text>().enabled = false;

                        newButton.Find("Image").GetComponent<Image>().enabled = true;
                        newButton.Find("Image").GetComponent<Image>().sprite = pairsImageImage[index].firstImage;

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        if (newButton.Find("SoundIcon")) newButton.Find("SoundIcon").GetComponent<Image>().enabled = false;

                        //newButton.GetComponent<Button>().interactable = false;

                        //newButton.GetComponent<MGTPlaySound>().sound = pairsImageImage[index].sound;

                        // Set the sound for both parts of the pair, so that when we compare them we can find which texts match
                        //newButton.Find("Text2").GetComponent<Text>().text = newButton.Find("Image").GetComponent<Image>().name;
                        //newButton.Find("Text2").GetComponent<Text>().text = newButton.Find("Image").GetComponent<Image>().sprite.name;

                        firstOfPair = false;

                        index--;
                    }
                    else
                    {
                        if (secondGroup)
                        {
                            // Put it inside the button grid
                            newButton.transform.SetParent(secondGroup);

                            // Set the scale to the default button's scale
                            newButton.localScale = defaultButton.localScale;

                            // Set the position to the default button's position
                            newButton.position = defaultButton.position;
                        }

                        newButton.Find("Image").GetComponent<Image>().enabled = true;


                        // Hide the image and show the text
                        newButton.Find("Text").GetComponent<Text>().enabled = false;

                        newButton.Find("Image").GetComponent<Image>().enabled = true;
                        newButton.Find("Image").GetComponent<Image>().sprite = pairsImageImage[index].secondImage;

                        // Set the name of the matching pair
                        newButton.name = "Pair" + index;

                        if (newButton.Find("SoundIcon")) newButton.Find("SoundIcon").GetComponent<Image>().enabled = false;

                        //newButton.GetComponent<MGTPlaySound>().sound = pairsImageImage[index].sound;

                        //newButton.Find("Image").GetComponent<Image>().sprite = pairsImageImage[index].image;

                        // Set the sound for both parts of the pair, so that when we compare them we can find which texts match
                        //newButton.Find("Text").GetComponent<Text>().text = newButton.Find("Image").GetComponent<Image>().name;
                        //newButton.Find("Text2").GetComponent<Text>().text = newButton.Find("Image").GetComponent<Image>().sprite.name;

                        pairsLeft++;

                        currentPair++;

                        firstOfPair = true;
                    }

                    // Deactivate the button until we actually need it
                    newButton.gameObject.SetActive(true);

                    // Select the object for keyboard/gamepad controls
                    if (eventSystem) eventSystem.SetSelectedGameObject(newButton.gameObject);

                    // Add the button to an array, so that we can shuffle the pairs, and later remove them when the level is completed
                    pairsArray[arrayIndex] = newButton;

                    arrayIndex++;

                    // If we passed through all the pairs in the game, stop creating them
                    if (currentPair >= levelPairs) break;
                }

                // Reset the pairs array index
                arrayIndex = 0;

                // Reset the pair check. The next time we click on an object it will be the first of a pair
                firstOfPair = true;

                // We will do several randomization passes on the pairs array, shuffling them
                int randomCount = pairsArray.Length;

                // Repeat the shuffle several times
                while (randomCount > 0)
                {
                    if (randomCount < pairsArray.Length)
                    {
                        int randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose one of the elements in the array randomly and put it at the start
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsFirstSibling();

                        randomIndex = Mathf.FloorToInt(Random.Range(0, randomCount));

                        // Choose another of the elements in the array randomly and put it at the end
                        if (pairsArray[randomIndex]) pairsArray[randomIndex].SetAsLastSibling();
                    }

                    randomCount--;
                }
            }
        }


        /// <summary>
        /// Runs the game over event and shows the game over screen
        /// </summary>
        IEnumerator GameOver(float delay)
        {
            isGameOver = true;

            yield return new WaitForSeconds(delay);

            //Show the game over screen
            if (gameOverCanvas)
            {
                //Show the game over screen
                gameOverCanvas.gameObject.SetActive(true);

                //Write the score text, if it exists
                if (gameOverCanvas.Find("ScoreTexts/TextScore")) gameOverCanvas.Find("ScoreTexts/TextScore").GetComponent<Text>().text = "SCORE " + score.ToString();

                //Check if we got a high score
                if (score > highScore)
                {
                    highScore = score;

                    //Register the new high score
                    PlayerPrefs.SetFloat(SceneManager.GetActiveScene().name + "HighScore", score);
                }

                //Write the high sscore text
                gameOverCanvas.Find("ScoreTexts/TextHighScore").GetComponent<Text>().text = "HIGH SCORE " + highScore.ToString();

                //If there is a source and a sound, play it from the source
                if (soundSource && soundGameOver) soundSource.GetComponent<AudioSource>().PlayOneShot(soundGameOver);
            }
            //wait and show ad
            yield return new WaitForSeconds(2f);
            adLoader.LoadAd();
            adLoader.ShowAd();
        }

        /// <summary>
        /// Runs the victory event and shows the victory screen
        /// </summary>
        IEnumerator Victory(float delay)
        {
            isGameOver = true;

            yield return new WaitForSeconds(delay);

            //Show the game over screen
            if (victoryCanvas)
            {
                //Show the victory screen
                victoryCanvas.gameObject.SetActive(true);

                // If we have a TextScore and TextHighScore objects, then we are using the single player victory canvas
                if (victoryCanvas.Find("ScoreTexts/TextScore") && victoryCanvas.Find("ScoreTexts/TextHighScore"))
                {
                    //Write the score text, if it exists
                    victoryCanvas.Find("ScoreTexts/TextScore").GetComponent<Text>().text = "SCORE " + score.ToString();

                    //Check if we got a high score
                    if (score > highScore)
                    {
                        highScore = score;

                        //Register the new high score
                        PlayerPrefs.SetFloat(SceneManager.GetActiveScene().name + "HighScore", score);
                    }

                    //Write the high sscore text
                    victoryCanvas.Find("ScoreTexts/TextHighScore").GetComponent<Text>().text = "HIGH SCORE " + highScore.ToString();
                }

                //If there is a source and a sound, play it from the source
                if (soundSource && soundVictory) soundSource.GetComponent<AudioSource>().PlayOneShot(soundVictory);
            }
        }

        /// <summary>
        /// Restart the current level
        /// </summary>
        void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Restart the current level
        /// </summary>
        void MainMenu()
        {
            SceneManager.LoadScene(mainMenuLevelName);
        }
    }
}