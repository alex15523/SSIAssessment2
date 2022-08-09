using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SSIAssessment2.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace SSIAssessment2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        private List<string> splitCLM05(string CLM05)
        {
            var splitCLM05 = CLM05.Split(":").ToList();
            var CLM05Segments = new List<string>();

            for(int i = 0; i < splitCLM05.Count; i++)
            {
                switch (i)
                {
                    case 0:
                        CLM05Segments.Add("CLM05 – 1 Composite Element (Represents Facility Type Code): " + splitCLM05[i]);
                        break;
                    case 1:
                        CLM05Segments.Add("CLM05 – 2 Composite Element (Represents Facility Code Qualifier): " + splitCLM05[i]);
                        break;
                    case 2:
                        CLM05Segments.Add("CLM05 – 3 Composite Element (Represents Claim Frequency Code):  " + splitCLM05[i]);
                        break;
                }
            }


            return CLM05Segments;
        }
        private (List<List<string>>, int) splitClaims(string fileConents)
        {
            //splitLines is set to result of splitting .837 file by lines
            List<string> splitLines = fileConents.Replace("\n", "").Split('\r').ToList<string>();
            int totalClaimChargeAmount = 0;

            //cLMClaims will be list of CLM lines
            var cLMClaims = new List<string>();

            foreach(var line in splitLines)
            {
                if(line.Length > 3 && line[0..3] == "CLM")
                {
                    cLMClaims.Add(line);
                }
            }

            //claimComponents will be a list of each CLM segment, each CLM segment will be a list of the breakdown of the segment
            var claimComponents = new List<List<string>>();

            foreach(var claim in cLMClaims)
            {
                //split each segment by the * delimeter
                var components = claim.Split("*").ToList();
                
                //find index that contains the termination character, ensure components of claim are within the proper range (first index to index that contains termination character)
                var terminatingIndex = components.FindIndex(x => x.Contains("~"));
                components = components.GetRange(0, terminatingIndex + 1);
                components[terminatingIndex] = components.Last().Replace("~", string.Empty);

                //cleanedComponents will be list of the breakdown of claims segment
                var cleanedComponents = new List<string>();

                //loop through each component of segment and add the proper row identifier
                for(int i = 0; i < components.Count; i++)
                {
                    switch (i)
                    {
                        case 0:
                            cleanedComponents.Add("Segment Identifier: " + components[i]);
                            break;
                        case 1:
                            cleanedComponents.Add("CLM01 Element (Represents Patient Account Number): " + components[i]);
                            break;
                        case 2:
                            //adds claim charge amount
                            totalClaimChargeAmount += Int32.Parse(components[i]);
                            cleanedComponents.Add("CLM02 Element (Represents Total Claim Charge Amount): " + components[i]);
                            break;
                        case 3:
                            cleanedComponents.Add("CLM03 Element (Not Used): " + components[i]);
                            break;
                        case 4:
                            cleanedComponents.Add("CLM04 Element (Not Used): " + components[i]);
                            break;
                        case 5:
                            //process CLM05 as a special case
                            //splitCLM05 will take a string as an input and output a list of the breakdown of the CLM05 component
                            cleanedComponents.AddRange(splitCLM05(components[i]));
                            break;
                        case 6:
                            cleanedComponents.Add("CLM06 (Represents Provider/Supplier Signature Indicator): " + components[i]);
                            break;
                        case 7:
                            cleanedComponents.Add("CLM07 (Represents Assignment/Plan Participation Code): " + components[i]);
                            break;
                        case 8:
                            cleanedComponents.Add("CLM08 (Represents Benefits Assignment Certification Indicator): " + components[i]);
                            break;
                        case 9:
                            cleanedComponents.Add("CLM09 (Represents Release of Information Indicator): " + components[i]);
                            break;

                    }
                }

                //add final processed segment to method output list
                claimComponents.Add(cleanedComponents);
            }

            return (claimComponents, totalClaimChargeAmount); 
        }

        //processes uploaded file and outputs results to Index view
        [HttpPost]
        public async Task<IActionResult> Index(IFormFile uploadedFile)
        {
            var fileNameExt = "";

            //sets fileNameExt to file format
            if (uploadedFile.FileName.Contains(".")) 
            {
                fileNameExt = uploadedFile.FileName.Split(".").Last();
            }
            else
            {
                ViewBag.ErrorMessage = "Please ensure file has the .837 format.";
                return View();
            }

            //returns to Index with ViewBag.ErrorMessage if improper file format
            if(fileNameExt != "837")
            {
                ViewBag.ErrorMessage = "Please ensure file has the .837 format.";
                return View();
            }

            //sets fileContents to contents of uploaded file
            string fileContents;
            using (var stream = uploadedFile.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                fileContents = await reader.ReadToEndAsync();
            }

            //splitClaims function will take string value of file contents and split CLM segments into a list and total claim charge amounts as a tuple
            //item1 = CLM segments list
            //item2 = total claim charge amounts
            var claimSegments = splitClaims(fileContents);

            ViewBag.UploadSuccess = true;

            return View("Index", claimSegments);
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
