using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace func
{
    public static class HttpExample
    {
        [FunctionName("HttpExample")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";


            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--no-sandbox");
            using (var driver = new ChromeDriver(chromeOptions))
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                var baseurl = Environment.GetEnvironmentVariable("BASE_URL");
                //Navigate to base website
                driver.Navigate().GoToUrl(baseurl);
                IReadOnlyList<IWebElement> boxEles = driver.FindElements(By.CssSelector(".box"));
                responseMessage = boxEles.ElementAt(0).Text;
                log.LogInformation(responseMessage);

                //Click on the navbar toggle
                IWebElement navbarToggler = driver.FindElement(By.CssSelector(".navbar-toggler.collapsed"));
                navbarToggler.Click();

                //Click on the login link
                var loginEleXpath = By.XPath("//a[contains(text(),'Login')]");
                wait.Until(ExpectedConditions.ElementIsVisible(loginEleXpath));
                IWebElement loginEle = driver.FindElement(loginEleXpath);
                log.LogInformation("Login button");
                log.LogInformation($"Login Displayed: {loginEle.Displayed}");
                log.LogInformation($"Login Enabled: {loginEle.Enabled}");
                loginEle.Click();

                //Wait for the B2C login page
                wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".intro")));
                IReadOnlyList<IWebElement> introEles = driver.FindElements(By.CssSelector(".intro"));
                log.LogInformation(introEles.ElementAt(0).Text);
                
                //fill out the form
                var testemail = Environment.GetEnvironmentVariable("TEST_EMAIL");
                var testpassword = Environment.GetEnvironmentVariable("TEST_PASSWORD");
                driver.FindElement(By.Id("email")).SendKeys(testemail);
                IWebElement passwordEle = driver.FindElement(By.Id("password"));
                passwordEle.SendKeys(testpassword);
                passwordEle.SendKeys(Keys.Enter);
                
                //Wait for login to complete
                wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".navbar-toggler.collapsed")));
                log.LogInformation("Login has completed");
                navbarToggler = driver.FindElement(By.CssSelector(".navbar-toggler.collapsed"));
                navbarToggler.Click();

                //Go to accounts
                var accountEleXpath = By.XPath("//a[contains(text(),'Accounts')]");
                wait.Until(ExpectedConditions.ElementIsVisible(accountEleXpath));
                IWebElement accountEle = driver.FindElement(accountEleXpath);
                accountEle.Click();
                log.LogInformation("Clicking on Accounts");

                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//h3[contains(text(),'Checking')]")));
                log.LogInformation("Accounts are visible");
                Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
                return new FileContentResult(ss.AsByteArray, "image/jpeg");

            }

            return new OkObjectResult(responseMessage);
        }
    }
}
