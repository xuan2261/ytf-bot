using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
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
        private readonly Logger logger = new("FacebookAutomation");

        // css selector to find the cookie accept button
        private static readonly By CssSelectorCookieAccept =
            By.CssSelector("[data-testid = 'cookie-policy-dialog-accept-button']");

        // css selector to find the login button
        private static readonly By CssSelectorLogInButton =
            By.CssSelector("[data-testid = 'royal_login_button']");

        // xpath selector to find element that opens the post to group dialog
        private static readonly By XPathSelectorWriteIntoGroup =
            By.XPath("//*[contains(text(),'Schreib etwas')]");

        // xpath selector to find button post to group
        private static readonly By XPathSelectorPostInGroup =
            By.XPath("//div[@aria-label='Posten'][@role='button']");

        // xpath selector for youtube preview
        private static readonly By XPathSelectorYoutubePreviewBox =
            By.XPath("//a[@role='link'][contains(.,'youtube')]");

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
        public FacebookAutomation(Logger logger = null)
        {
            if (logger != null)
            {
                this.logger = logger;
            }

            try
            {
                var options = new ChromeOptions();
                options.AddArgument("--disable-notifications");
                options.AddArgument("--window-size=1500,1200");
                options.AddExcludedArgument("enable-logging");

                this.webDriver = new ChromeDriver(AppDomain.CurrentDomain.BaseDirectory, options);
            }
            catch (Exception e)
            {
                this.logger.LogError($"Error when initializing FacebookAutomation. {Environment.NewLine} Exception: {e}");
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
                this.logger.LogError("Error while Login. ExceptionMessage: " + e);
                throw;
            }
        }

        /// <summary>
        /// Synchronous method to publish textual content into a Facebook group.
        /// </summary>
        public void PublishTextContentInFaceBookGroup(string groupId, string textToPublish)
        {
            try
            {
                // Navigate to group
                this.webDriver.Url = $"https://www.facebook.com/groups/{groupId}";

                // Open the dialog for posting content
                ClickAndWaitForClickableElement(this.webDriver, XPathSelectorWriteIntoGroup);

                // Wait until dialog is open by checking for existence of button "Posten"
                WaitForElementToAppear(this.webDriver, XPathSelectorPostInGroup);

                // Write the text of the message into the dialog. Note: this is not an input element.
                var sendKeysAction = new Actions(this.webDriver).SendKeys(textToPublish);
                sendKeysAction.Perform();

                // Wait for youtube preview box to appear
                WaitForElementToAppear(this.webDriver, XPathSelectorYoutubePreviewBox);

                ClickElementAndWaitForExcludingFromDom(this.webDriver, XPathSelectorPostInGroup);
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Clicks an element (i.g button in a modal dialog, disappearing loginButton) and waits until this element isn't part of
        /// the DOM anymore.
        /// In other words this method waits after clicking until you are logged in.
        /// </summary>
        private void ClickElementAndWaitForExcludingFromDom(IWebDriver driver, By elementLocator, int timeOut = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeOut));
                var elements = driver.FindElements(elementLocator);
                if (elements.Count == 0)
                {
                    throw new NoSuchElementException(
                        "No elements " + elementLocator + " ClickElementAndWaitForExcludingFromDOM");
                }

                var element = elements.First(e => e.Displayed);
                element.Click();

                // Releases when element isn't part of the DOM anymore (Dialog i.g.).
                wait.Until(ExpectedConditions.StalenessOf(element));
            }
            catch (NoSuchElementException e)
            {
                this.logger.LogError("Element with locator: '" + elementLocator + "' was not found. Exception.Message: " + e.Message);
                throw;
            }
            catch (Exception e)
            {
                this.logger.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Waits for an element to appear in the DOM.
        /// </summary>
        private void WaitForElementToAppear(IWebDriver driver, By elementLocator, int timeOut = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeOut));
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(elementLocator));
            }
            catch (NoSuchElementException e)
            {
                this.logger.LogError("Element with locator: '" + elementLocator + "' was not found. Exception.Message: " + e.Message);
                throw;
            }
            catch (Exception e)
            {
                this.logger.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Unclear.
        /// Clicks an element and waits until its... clickable?
        /// </summary>
        private void ClickAndWaitForClickableElement(IWebDriver driver, By elementLocator, int timeOut = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeOut));
                var elements = driver.FindElements(elementLocator);
                if (elements.Count == 0)
                {
                    throw new NoSuchElementException(
                        "No elements " + elementLocator + " ClickElementAndWaitForExcludingFromDOM");
                }

                var element = elements.First(e => e.Displayed);
                element.Click();
                wait.Until(ExpectedConditions.ElementToBeClickable(element));
            }
            catch (NoSuchElementException e)
            {
                this.logger.LogError("Element with locator: '" + elementLocator + "' was not found. Exception.Message: " + e.Message);
                throw;
            }
            catch (Exception e)
            {
                this.logger.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Make sure this method to be called, use the ctor of this class in s "using block".
        /// </summary>
        public void Dispose()
        {
            this.webDriver.Close();
            this.webDriver.Quit();
        }
    }
}