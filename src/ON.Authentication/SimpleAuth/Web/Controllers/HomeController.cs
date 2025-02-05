﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ON.Authentication.SimpleAuth.Web.Models;
using ON.Authentication.SimpleAuth.Web.Services;

namespace ON.Authentication.SimpleAuth.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly UserService userService;

        public HomeController(ILogger<HomeController> logger, UserService userService)
        {
            this.logger = logger;
            this.userService = userService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(SettingsGet));
        }

        [HttpGet("/changepassword")]
        public IActionResult ChangePasswordGet()
        {
            var vm = new ChangePasswordViewModel();

            return View("ChangePassword", vm);
        }

        [HttpPost("/changepassword")]
        public async Task<IActionResult> ChangePasswordPost(ChangePasswordViewModel vm)
        {
            vm.ErrorMessage = vm.SuccessMessage = "";
            if (!ModelState.IsValid)
            {
                vm.ErrorMessage = ModelState.Values.FirstOrDefault(v => v.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                                        ?.Errors?.FirstOrDefault()?.ErrorMessage;
                return View("ChangePassword", vm);
            }

            if (vm.OldPassword == vm.NewPassword)
                return View("ChangePassword", new ChangePasswordViewModel { ErrorMessage = "Old password and new password are the same" });

            var error = await userService.ChangePasswordCurrentUser(vm);
            switch (error)
            {
                case Fragments.Authentication.ChangeOwnPasswordResponse.Types.ErrorType.NoError:
                    return View("ChangePassword", new ChangePasswordViewModel { SuccessMessage = "Settings updated Successfully" });
                case Fragments.Authentication.ChangeOwnPasswordResponse.Types.ErrorType.BadOldPassword:
                    return View("ChangePassword", new ChangePasswordViewModel { ErrorMessage = "Old password is not correct" });
                case Fragments.Authentication.ChangeOwnPasswordResponse.Types.ErrorType.BadNewPassword:
                    return View("ChangePassword", new ChangePasswordViewModel { ErrorMessage = "New password is not valid" });
                case Fragments.Authentication.ChangeOwnPasswordResponse.Types.ErrorType.UnknownError:
                default:
                    return RedirectToAction(nameof(Error));
            }
        }

        [AllowAnonymous]
        [HttpGet("/login")]
        public IActionResult LoginGet()
        {
            return View("Login");
        }

        [AllowAnonymous]
        [HttpPost("/login")]
        public async Task<IActionResult> LoginPost(LoginViewModel vm)
        {
            vm.ErrorMessage = "";

            if (!ModelState.IsValid)
            {
                return View("Login", vm);
            }

            var token = await userService.AuthenticateUser(vm.LoginName, vm.Password);
            if (string.IsNullOrEmpty(token))
            {
                vm.ErrorMessage = "Your login/password is not correct.";
                return View("Login", vm);
            }

            Response.Cookies.Append(JwtExtensions.JWT_COOKIE_NAME, token, new CookieOptions()
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddDays(21),
                IsEssential = true,
            });
            return Redirect("/");
        }

        [AllowAnonymous]
        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(JwtExtensions.JWT_COOKIE_NAME);
            return RedirectToAction(nameof(LoginGet));
        }

        [HttpGet("/settings/refreshtoken")]
        public async Task<IActionResult> RefreshToken(string url)
        {
            var token = await userService.RenewToken();
            if (string.IsNullOrEmpty(token))
            {
                return Redirect("/logout");
            }

            Response.Cookies.Append(JwtExtensions.JWT_COOKIE_NAME, token, new CookieOptions()
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddDays(21),
                IsEssential = true,
            });

            return Redirect(url);
        }

        [AllowAnonymous]
        [HttpGet("/register")]
        public IActionResult RegisterGet()
        {
            return View("Register");
        }

        [AllowAnonymous]
        [HttpPost("/register")]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.ErrorMessage = ModelState.Values.FirstOrDefault()?.Errors?.FirstOrDefault()?.ErrorMessage;
                return View(vm);
            }

            var res = await userService.CreateUser(vm);

            if (res.Error == Fragments.Authentication.CreateUserResponse.Types.ErrorType.UserNameTaken)
            {
                vm.ErrorMessage = "The User Name is already taken.";
                return View(vm);
            }

            if (res.Error == Fragments.Authentication.CreateUserResponse.Types.ErrorType.UnknownError)
            {
                vm.ErrorMessage = "An error occured creating your account.";
                return View(vm);
            }

            Response.Cookies.Append(JwtExtensions.JWT_COOKIE_NAME, res.BearerToken, new CookieOptions()
            {
                HttpOnly = true
            });
            return RedirectToAction(nameof(SettingsGet));
        }

        [HttpGet("/settings")]
        public async Task<IActionResult> SettingsGet()
        {
            var user = await userService.GetCurrentUser();
            if (user == null)
                return RedirectToAction(nameof(Error));

            var vm = new SettingsViewModel(user);

            return View("Settings", vm);
        }

        [HttpPost("/settings")]
        public async Task<IActionResult> SettingsPost(SettingsViewModel vm)
        {
            vm.ErrorMessage = vm.SuccessMessage = "";
            if (!ModelState.IsValid)
            {
                vm.ErrorMessage = ModelState.Values.FirstOrDefault(v => v.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                                        ?.Errors?.FirstOrDefault()?.ErrorMessage;
                return View("Settings", vm);
            }

            var res = await userService.ModifyCurrentUser(vm);
            if (!string.IsNullOrEmpty(res.Error))
            {
                vm.ErrorMessage = res.Error;
                return View("Settings", vm);
            }

            if (!string.IsNullOrEmpty(res.BearerToken))
            {
                Response.Cookies.Append(JwtExtensions.JWT_COOKIE_NAME, res.BearerToken, new CookieOptions()
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(21)
                });
            }


            var user = await userService.GetCurrentUser();
            if (user == null)
                return RedirectToAction(nameof(Error));

            vm = new SettingsViewModel(user)
            {
                SuccessMessage = "Settings updated Successfully"
            };

            return View("Settings", vm);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
