using ABCRetails.Models;
using ABCRetailsFunctions.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly IFunctionsApi _functionsApi;

        public FileUploadController(IFunctionsApi functionsApi)
        {
            _functionsApi = functionsApi;
        }

        public ActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        // Use the Functions API to upload proof of payment
                        var fileName = await _functionsApi.UploadProofOfPaymentAsync(
                            model.ProofOfPayment,
                            model.OrderId,
                            model.CustomerName);

                        TempData["Success"] = $"File '{fileName}' uploaded successfully via Functions API!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }
            return View(model);
        }
    }
}