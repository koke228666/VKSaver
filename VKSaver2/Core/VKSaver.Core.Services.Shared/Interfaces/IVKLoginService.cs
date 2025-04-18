﻿using ModernDev.InTouch;
using System;
using System.Threading.Tasks;
using VKSaver.Core.Models.Common;

namespace VKSaver.Core.Services.Interfaces
{
    /// <summary>
    /// Проедставляет сервис авторизации ВКонтакте.
    /// </summary>
    public interface IVKLoginService
    {
        /// <summary>
        /// Просиходит при успешной авторизации пользователя.
        /// </summary>
        event TypedEventHandler<IVKLoginService, EventArgs> UserLogin;
        /// <summary>
        /// Происходит при выходе пользователя.
        /// </summary>
        event TypedEventHandler<IVKLoginService, EventArgs> UserLogout;

        /// <summary>
        /// Возвращает идентификатор текущего авторизованного пользователя.
        /// </summary>
        long UserID { get; }

        /// <summary>
        /// Возвращает токен авторизации текущего пользователя.
        /// </summary>
        string Token { get; }

        /// <summary>
        /// Возвращает значение, выполнена ли авторизация.
        /// </summary>
        bool IsAuthorized { get; }

        /// <summary>
        /// Возвращает адрес для oAuth-авторизации ВКонтакте.
        /// </summary>
        string GetOAuthUrl();

        /// <summary>
        /// Выполняет авторизацию по полученному токену.
        /// </summary>
        void Login(APISession session);

        /// <summary>
        /// Выполняет авторизацию по имени пользователя и паролю.
        /// </summary>
        /// <param name="userName">Имя пользователя.</param>
        /// <param name="password">Пароль.</param>
        Task<VKDirectAuthResponse> Login(string userName, string password, string captchaSid = null, string captchaKey = null, string code = null, bool forseSms = false);

        /// <summary>
        /// Возвращает ключ доступа к ВКонтакте из redireted-пути oAuth.
        /// </summary>
        /// <param name="oAuthUrl">Путь.</param>
        APISession GetAccessTokenFromUrl(string oAuthUrl);

        /// <summary>
        /// Возвращает данные авторизации ВКонтакте в общем виде.
        /// </summary>
        IServiceAuthorization GetServiceAuthorization();

        /// <summary>
        /// Завершает авторизацию.
        /// </summary>
        void Logout();

        void InitializeInTouch();
    }
}
