﻿using DGP.Genshin.Models.MiHoYo;
using DGP.Genshin.Models.MiHoYo.BBSAPI;
using DGP.Genshin.Models.MiHoYo.BBSAPI.Post;
using DGP.Genshin.Models.MiHoYo.Request;
using DGP.Snap.Framework.Extensions.System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DGP.Genshin.Services
{
    internal class MiHoYoBBSService
    {
        public const string Referer = @"https://bbs.mihoyo.com/";
        public const string BaseUrl = @"https://bbs-api.mihoyo.com/user/wapi";
        public const string PostBaseUrl = @"https://bbs-api.mihoyo.com/post/wapi";

        public MiHoYoBBSService()
        {
            this.Log("initialized");
        }
        public async Task<UserInfo> GetUserFullInfoAsync()
        {
            string cookie = await CookieManager.GetCookieAsync();
            Requester requester = new Requester(new RequestOptions
            {
                {"DS", DynamicSecretProvider.Create() },
                {"x-rpc-app_version", DynamicSecretProvider.AppVersion },
                {"User-Agent", RequestOptions.CommonUA },
                {"x-rpc-device_id", RequestOptions.DeviceId },
                {"Accept", RequestOptions.Json },
                {"x-rpc-client_type", "4" },
                {"Referer",Referer },
                {"Cookie", cookie }
            });
            Response<UserInfoWrapper> resp = await Task.Run(() =>
            requester.Get<UserInfoWrapper>($"{BaseUrl}/getUserFullInfo?gids=2"));
            return resp.ReturnCode == 0 ? resp.Data.UserInfo : null;
        }

        public async Task<List<Post>> GetOfficialRecommendedPostsAsync()
        {
            string cookie = await CookieManager.GetCookieAsync();
            Requester requester = new Requester(new RequestOptions
            {
                {"DS", DynamicSecretProvider.Create() },
                {"x-rpc-app_version", DynamicSecretProvider.AppVersion },
                {"User-Agent", RequestOptions.CommonUA },
                {"x-rpc-device_id", RequestOptions.DeviceId },
                {"Accept", RequestOptions.Json },
                {"x-rpc-client_type", "4" },
                {"Referer",Referer },
                {"Cookie", cookie }
            });
            Response<PostWrapper> resp = await Task.Run(() =>
            requester.Get<PostWrapper>($"{PostBaseUrl}/getOfficialRecommendedPosts?gids=2"));
            return resp.Data.List;
        }

        public async Task<dynamic> GetPostFullAsync(string postId)
        {
            string cookie = await CookieManager.GetCookieAsync();
            Requester requester = new Requester(new RequestOptions
            {
                {"DS", DynamicSecretProvider.Create() },
                {"x-rpc-app_version", DynamicSecretProvider.AppVersion },
                {"User-Agent", RequestOptions.CommonUA },
                {"x-rpc-device_id", RequestOptions.DeviceId },
                {"Accept", RequestOptions.Json },
                {"x-rpc-client_type", "4" },
                {"Referer",Referer },
                {"Cookie", cookie }
            });
            Response<dynamic> resp = await Task.Run(() =>
            requester.Get<dynamic>($"{PostBaseUrl}/getPostFull?gids=2&post_id={postId}&read=1"));
            return resp.Data;
        }
    }
}