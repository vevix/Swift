﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;
using RestSharp;
using Swift.API.Clients.Interfaces;
using Swift.API.Extensions;
using Swift.API.Http;
using Swift.API.Http.Interfaces;
using Swift.API.Models.Responses;

namespace Swift.API.Clients {
    public class UsersClient : IUsersClient {
        private readonly IConnection _connection;

        public UsersClient(IConnection connection) {
            _connection = connection;
        }

        #region IUsersClient Members

        public IObservable<User> GetInfo(string username) {
            var parameters = new List<Parameter> {
                new Parameter { Name = "username", Value = username, Type = ParameterType.UrlSegment }
            };
            var ret = _connection.ExecuteRequest("users/{username}", parameters)
                .SelectMany(x => x.ExpectStatus(HttpStatusCode.OK));
            return ret.Deserialize<User>();
        }

        public IObservable<string> Authenticate(string username, string password) {
            var parameters = new List<Parameter> {
                new Parameter { Name = "username", Value = username, Type = ParameterType.QueryString },
                new Parameter { Name = "password", Value = password, Type = ParameterType.QueryString }
            };

            return _connection.ExecuteRaw("users/authenticate", parameters, Method.POST)
                .SelectMany(x => x.ExpectStatus(HttpStatusCode.Created))
                .SelectMany(x => Observable.Return(x.Content));
        }

        #endregion
    }
}