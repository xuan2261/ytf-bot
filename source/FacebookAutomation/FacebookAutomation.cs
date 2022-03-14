using System;
using System.IO;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SimpleLogger;

namespace FacebookAutomation
{
    /// <summary>
    /// This automates the LoginProcess into Facebook via Chrome and allows to public content in Groups the user is member.
    /// </summary>
    public class FacebookAutomation : IDisposable
    {
        private readonly Logger logger;

        /// <summary>
        /// The WorkDir contains all the files needed for the FacebookAutomation:
        /// 1.  Subfolders in working directory within lists of files within VideoMetaDataFull per video to check which videos
        ///     have been published on the channels.
        /// 2.  One 'ListOfProcessedFiles' file per task to check which videos from the VideoMetaDataFull files have
        /// already been processed by a bot in a specific task.
        /// </summary>
        public readonly string WorkDir;

        // http://xpather.com/
        // https://stackoverflow.com/questions/69909751/why-is-xpath-containstext-substring-not-working-as-expected?noredirect=1&lq=1
        // https://selenium-python.readthedocs.io/locating-elements.html
        // https://www.guru99.com/using-contains-sbiling-ancestor-to-find-element-in-selenium.html#:~:text=contains()%20in%20Selenium%20is,()%20function%20throughout%20the%20webpage.

        // css selector to find the cookie accept button
        private static readonly Tuple<string, By> CssSelectorCookieAccept = 
            new(nameof(CssSelectorCookieAccept),
                By.CssSelector("[data-testid = 'cookie-policy-manage-dialog-accept-button']"));

        // css selector to find the login button
        private static readonly Tuple<string, By> CssSelectorLogInButton =
            new(nameof(CssSelectorLogInButton),
                By.CssSelector("[data-testid = 'royal_login_button']"));

        // xpath selector to find element that opens the post to group dialog
        private static readonly Tuple<string, By> OpenPostToGroupDialogSelector =
            new(nameof(OpenPostToGroupDialogSelector),
                By.XPath("//div[@data-pagelet='GroupInlineComposer'] //span[(contains(.,'Schreib etwas') or contains(., 'Was machst du gerade')) and contains(@style,'webkit-box')]"));

        private static readonly Tuple<string, By> OpenPostToProfileDialogSelector =
            new(nameof(OpenPostToGroupDialogSelector),
                By.XPath("//div[@data-pagelet='ProfileComposer'] //span[contains(., 'Was machst du gerade') and contains(@style,'webkit-box')]"));


        // xpath selector to find button post to group
        private static readonly Tuple<string, By> PostToGroupButtonSelector =
            new(nameof(PostToGroupButtonSelector),
                By.XPath("//div[@aria-label='Posten' and @role='button']"));

        // xpath selector for youtube preview box in that dialog for publishing contents in a group
        private static readonly Tuple<string, By> XPathSelectorYoutubePreviewBox =
            new(nameof(XPathSelectorYoutubePreviewBox),
                By.XPath("//a[@role='link' and @target='_blank' and contains(.,'youtube')]"));

        // beinhaltet link, findet so aber 2 Elemente
        //a[@role='link' and @target='_blank' and contains(@href,'youtube') and contains(@href,'PyAexdhNXjY')] 


        // id selector to find input box for email
        private static readonly By IdSelectorEmail =
            By.Id("email");

        // id selector to find input box for password
        private static readonly By IdSelectorPassword =
            By.Id("pass");

        // WebDriver within browser and all test functionality
        private readonly IWebDriver webDriver;

        /// <summary>
        /// Ctor.
        /// Initiate the webDriver.
        /// </summary>
        public FacebookAutomation(string workDir, Logger theLogger = null)
        {
            this.logger = theLogger ?? new Logger("FacebookAutomation.log");

            try
            {
                if (!Directory.Exists(workDir)) Directory.CreateDirectory(workDir);
                this.WorkDir = workDir;

                var options = new ChromeOptions();
                options.AddArgument("--disable-notifications");
                options.AddArgument("--window-size=1500,1200");
                options.AddArgument("--headless");
                options.AddExcludedArgument("enable-logging");

                this.webDriver = new ChromeDriver(options);
            }
            catch (Exception e)
            {
                this.logger.LogError($"Error when initializing FacebookAutomation. {Environment.NewLine} Exception: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Synchronous method to login Facebook.
        /// </summary>
        /// <param name="username">A valid user name</param>
        /// <param name="password">A valid password</param>
        public void Login(string username, string password)
        {
            try
            {
                // Navigate to Facebook
                this.webDriver.Url = "https://www.facebook.com/";

                Thread.Sleep(TimeSpan.FromSeconds(1));

                // Accept the Cookies and what not
                ClickElementAndWaitForExcludingFromDom(this.webDriver, CssSelectorCookieAccept);

                // Find the username field (Facebook calls it "email") and enter value
                var input = this.webDriver.FindElement(IdSelectorEmail);
                input.SendKeys(username);

                // Find the password field and enter value
                input = this.webDriver.FindElement(IdSelectorPassword);
                input.SendKeys(password);

                // Click on the login button
                ClickElementAndWaitForExcludingFromDom(this.webDriver, CssSelectorLogInButton);
            }
            catch (Exception e)
            {
                this.logger.LogError("Error while Login. ExceptionMessage: " + e.Message);
                throw;
            }
        }

        public bool PublishToProfile(string textToPublish)
        {
            // TODO: Implementiere mich!
            return false;
        }

        /// <summary>
        /// Synchronous method to publish textual content into a Facebook group.
        /// </summary>
        public bool PublishToGroup(Group fbGroup, string textToPublish)
        {
            try
            {
                // Navigate to group
                this.webDriver.Url = $"https://www.facebook.com/groups/{fbGroup.GroupId}";

                // Wait until that post to group thing was hung into the dom
                if (!RepeatFunction(WaitForElementToAppear, this.webDriver, OpenPostToGroupDialogSelector))
                {
                    return false;
                }

                // Open the dialog for posting content
                if (!RepeatFunction(ClickAndWaitForClickableElement, this.webDriver, OpenPostToGroupDialogSelector))
                {
                    return false;
                }

                // Wait until dialog is open by checking for existence of button "Posten"
                if (!RepeatFunction(WaitForElementToAppear, this.webDriver, PostToGroupButtonSelector))
                {
                    return false;
                }

                // Write the text of the message into the dialog. Note: this is not an input element.
                var sendKeysAction = new Actions(this.webDriver).SendKeys(textToPublish);
                sendKeysAction.Perform();

                // Wait for youtube preview box to appear
                if (!RepeatFunction(WaitForElementToAppear, this.webDriver, XPathSelectorYoutubePreviewBox))
                {
                    return false;
                }

                // Click post to group and wait until box was excluded from dom
                if (!RepeatFunction(ClickElementAndWaitForExcludingFromDom, this.webDriver, PostToGroupButtonSelector))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Make sure this method to be called, use the ctor of this class in a "using block".
        /// </summary>
        public void Dispose()
        {
            this.webDriver.Close();
            this.webDriver.Quit();
        }

        /// <summary>
        /// Clicks an element (i.g button in a modal dialog, disappearing loginButton) and waits until this element isn't part of
        /// the DOM anymore.
        /// In other words this method waits after clicking until you are logged in.
        /// </summary>
        private bool ClickElementAndWaitForExcludingFromDom(IWebDriver driver, Tuple<string, By> elementLocator, int timeOut = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeOut));
                var elements = driver.FindElements(elementLocator.Item2);
                if (elements.Count == 0)
                {
                    throw new NoSuchElementException(
                        "No elements " + elementLocator + " ClickElementAndWaitForExcludingFromDom");
                }

                var element = elements.First(e => e.Displayed);
                element.Click();

                // Releases when element isn't part of the DOM anymore (Dialog i.g.).
                wait.Until(ExpectedConditions.StalenessOf(element));
            }
            catch (Exception e)
            {
                this.logger.LogError(GetLogMessage(e, elementLocator));
                return false;
            }
            
            Thread.Sleep(TimeSpan.FromSeconds(2));
            return true;
        }

        /// <summary>
        /// Waits for an element to appear in the DOM.
        /// </summary>
        private bool WaitForElementToAppear(IWebDriver driver, Tuple<string, By> elementLocator, int timeOut = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeOut));
                wait.Until(ExpectedConditions.ElementExists(elementLocator.Item2));
            }
            catch (Exception e)
            {
                this.logger.LogError(GetLogMessage(e, elementLocator));
                return false;
            }

            Thread.Sleep(TimeSpan.FromSeconds(2));
            return true;
        }

        /// <summary>
        /// Unclear.
        /// Clicks an element and waits until its... clickable?
        /// </summary>
        private bool ClickAndWaitForClickableElement(IWebDriver driver, Tuple<string, By> elementLocator, int timeOut = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeOut));
                var elements = driver.FindElements(elementLocator.Item2);
                if (elements.Count == 0)
                {
                    throw new NoSuchElementException(
                        "No elements " + elementLocator + " ClickAndWaitForClickableElement");
                }

                var element = elements.First(e => e.Displayed);
                element.Click();
                wait.Until(ExpectedConditions.ElementToBeClickable(element));
            }
            catch (Exception e)
            {
                this.logger.LogError(GetLogMessage(e, elementLocator));
                return false;
            }

            Thread.Sleep(TimeSpan.FromSeconds(2));
            return true;
        }

        private string GetLogMessage(Exception e, Tuple<string, By> elementLocator)
        {
            var message = e.Message + Environment.NewLine;
            message += "Locator not found: '" + elementLocator.Item1 + Environment.NewLine;
            message += "Locator criteria:" + elementLocator.Item2.Criteria;
            return message;
        }

        /// <summary>
        /// Repeat the function call up to repeatCountTimes.
        /// </summary>
        /// <param name="repeatCount">Don't set it higher than 3</param>
        /// <param name="function">This function s called</param>
        /// <param name="driver">Function argument 1</param>
        /// <param name="elementSelector">Function argument 2</param>
        /// <param name="timeout">Function argument 3</param>
        /// <returns></returns>
        private bool RepeatFunction(Func<IWebDriver, Tuple<string, By>, int, bool> function, 
                                    IWebDriver driver, 
                                    Tuple<string, By> elementSelector, 
                                    int timeout = 10,
                                    int repeatCount = 2)
        {
            var counter = 0;
            var result = false;
            while (counter < repeatCount && result != true)
            {
                result = function(driver, elementSelector, timeout);
                counter++;
            }
            return result;
        }

        /// <summary>
        /// Not yet in use, but it works.
        /// Replace content of a span.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="content"></param>
        /// <param name="newContent"></param>
        /// <param name="timeOut"></param>
        /// <exception cref="NoSuchElementException"></exception>
        private void FindSpanAndReplaceContent(IWebDriver driver, string content, string newContent, int timeOut = 10)
        {
            var elementLocator = By.XPath($"//span[contains(text(),'{content}')]");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeOut));
            var elements = driver.FindElements(elementLocator);
            if (elements.Count == 0)
            {
                throw new NoSuchElementException(
                    "No elements " + elementLocator + " ClickElementAndWaitForExcludingFromDom");
            }
            var element = elements.First(e => e.Displayed);

            driver.ExecuteJavaScript($"arguments[0].innerText = '{newContent}'", element);

            // Das hier brauchts vermutlich nicht.
            // Releases when element isn't part of the DOM anymore (Dialog i.g.).
            //wait.Until(ExpectedConditions.StalenessOf(element));
        }
    }
}