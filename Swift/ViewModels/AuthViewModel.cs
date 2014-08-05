﻿using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ReactiveUI;
using Splat;
using Swift.API;
using Swift.API.Exceptions;
using Swift.Extensions;
using Swift.Helpers;
using Swift.Models;

namespace Swift.ViewModels {
    public class AuthViewModel : ReactiveObject, ISupportsActivation {
        private string _password;
        private string _username;
        public ReactiveCommand<IBitmap> AvatarCommand { get; private set; }
        public ReactiveCommand<string> AuthCommand { get; private set; }
        public ReactiveCommand<object> RegistrationCommand { get; private set; }
        public ReactiveCommand<object> ResetPasswordCommand { get; private set; } 

        public string Username {
            get { return _username; }
            set { this.RaiseAndSetIfChanged(ref _username, value); }
        }

        public string Password {
            get { return _password; }
            set { this.RaiseAndSetIfChanged(ref _password, value); }
        }

        public string RegistrationUrl {
            get { return "http://hummingbird.me/users/sign_up"; }
        }

        public string ResetUrl {
            get { return "http://hummingbird.me/users/password/new"; }
        }

        public AuthViewModel() {
            Activator = new ViewModelActivator();
            AvatarCommand = ReactiveCommand.CreateAsyncObservable(_ => GetAvatar());
            AuthCommand = ReactiveCommand.CreateAsyncObservable(this.WhenAnyValue(x => x.Username, x => x.Password,
                (u, p) => !u.Empty() && !p.Empty()), _ => Authenticate());
            RegistrationCommand = ReactiveCommand.Create();
            ResetPasswordCommand = ReactiveCommand.Create();

            this.WhenActivated(d => {
                d(AuthCommand.Subscribe(token => {
                    var account = Service.Get<Account>();
                    account.Username = Username;
                    account.Token = token;
                    account.Save();

                    // change the content over to the app
                    var vm = Service.Get<MainWindowViewModel>();
                    //vm.Content = Service.Get<>()
                }));

                d(AuthCommand.ThrownExceptions.Select(x => {
                    Debug.WriteLine(x);
                    if (x is ApiException) {
                        var ex = x as ApiException;
                        if (ex.StatusCode == HttpStatusCode.Unauthorized) {
                            return new UserError("Check your credentials, and try again.");
                        }
                    }
                    return new UserError("There was an issue authenticating with Hummingbird.");
                }).SelectMany(UserError.Throw).Subscribe(_ => {
                    Username = "";
                    Password = "";

                    // will set the avatar back to blank hummingbird
                    AvatarCommand.Execute(null);
                }));
            });
        }

        #region ISupportsActivation Members

        public ViewModelActivator Activator { get; private set; }

        #endregion

        private IObservable<IBitmap> GetAvatar() {
            var @default = BitmapLoader.Current.LoadFromResource(
                "pack://application:,,,/Swift;component/Resources/avatar.png", 100, 100).ToObservable();

            return _username.Empty()
                ? @default
                : Observable.StartAsync(async () => {
                    var client = Service.Get<HummingbirdClient>();
                    var user = await client.Users.GetInfo(Username);
                    return user.Avatar;
                }).SelectMany(url => ImageCache.Get(url, 100, 100)).Catch(@default);
        }

        private IObservable<string> Authenticate() {
            var client = Service.Get<HummingbirdClient>();
            return client.Users.Authenticate(Username, Password);
        }
    }
}