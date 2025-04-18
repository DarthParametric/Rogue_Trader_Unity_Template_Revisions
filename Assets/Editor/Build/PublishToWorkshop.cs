using Steamworks;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Threading;
using System.Xml;
using System;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using UnityEditor;

namespace OwlcatModification.Editor.Build 
{
    public static class PublishToWorkshop 
    {
        public const uint GameAppId = 2186680;
        private static AppId_t AppId;
        private static Modification modification;
        private static bool needRunCallbacks = false;
        private static bool hasThumbnail = true;
        
        public static ReturnCode Publish(Modification modification) 
        {
            AppId = new(GameAppId);
            PublishToWorkshop.modification = modification;
            var ImagePath = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(modification)), modification.Settings.RelativeThumbnailPath);
            var FilesPath = modification.GetFinalBuildPath();
            Debug.Log($"ImageFile: {ImagePath}");
            Debug.Log($"BuildFiles: {FilesPath}");
            bool success;
            try {
                if (modification == null) 
                {
                    Debug.LogError($"Modification Asset is null?");
                    return ReturnCode.Exception;
                }
                if (!new FileInfo(ImagePath).Exists) 
                {
                    Debug.LogError($"Can't find image file at: {ImagePath}");
                    hasThumbnail = false;
                }
                if (!new DirectoryInfo(FilesPath).Exists) 
                {
                    Debug.LogError($"Can't find zip with build artifacts at: {FilesPath}");
                    return ReturnCode.Exception;
                }
                AppId = new(GameAppId);
                Debug.Log($"SteamAPI Initialized?: {SteamAPI.Init()}");
                success = CreateWorkshopItemIfNecessary(ImagePath, FilesPath);
                SteamAPI.Shutdown();
            }
            catch (Exception ex) {
                Debug.LogError(ex.ToString());
                success = false;
            }
            return success ? ReturnCode.Success : ReturnCode.Exception;
        }

        private static string modTitle;
        private static string description;
        private static string imagePath;
        private static string buildPath;
        private static PublishedFileId_t modID;
        private static bool failed = false;
        private static bool CreateWorkshopItemIfNecessary(string PathToImage, string PathToBuildFiles) 
        {
            description = modification.Settings.WorkshopDescription ?? modification.Manifest.Description;
            buildPath = PathToBuildFiles;
            var imageInfo = new FileInfo(PathToImage);
            imagePath = imageInfo.FullName;
            if (imageInfo.Exists) 
            {
                Debug.Log($"Image size: {imageInfo.Length / 1024} KB");
                if (imageInfo.Length > 900 * 1024) {
                    Debug.LogWarning("Image appears to be larger than 900 KB, Steam doesn't accept previews larger than 1 MB.");
                }
            }
            modTitle = modification.Manifest.DisplayName ?? "";
            Debug.Log($"Mod title: {modTitle}");
            if (!ulong.TryParse(modification.Settings.WorkshopId, out var tmpModID)) 
            {
                Debug.Log($"No WorkshopId found, creating new...");
                SteamAPICall_t callHandle = SteamUGC.CreateItem(AppId, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
                CallResult<CreateItemResult_t> createItemResult = CallResult<CreateItemResult_t>.Create(OnCreateItemResult);
                createItemResult.Set(callHandle);
                needRunCallbacks = true;
                return SteamRunCallbacks(true);
            }
            else 
            {
                modID = new(tmpModID);
                return PublishWorkshopItem();
            }
        }

        private static void OnCreateItemResult(CreateItemResult_t callback, bool bIOFailure) 
        {
            if (bIOFailure || callback.m_eResult != EResult.k_EResultOK) 
            {
                failed = true;
                Debug.LogError("Failed to create item on Steam Workshop");
            }
            else 
            {
                failed = false;
                Debug.Log("Item created on Steam Workshop with ID: " + callback.m_nPublishedFileId.m_PublishedFileId);
                modID = callback.m_nPublishedFileId;
                modification.Settings.WorkshopId = callback.m_nPublishedFileId.m_PublishedFileId.ToString();
                EditorUtility.SetDirty(modification);
                AssetDatabase.SaveAssets();
            }
            needRunCallbacks = false;
        }
        private static bool PublishWorkshopItem() 
        {
            var updateHandle = SteamUGC.StartItemUpdate(AppId, modID);
            if (!SteamUGC.SetItemTitle(updateHandle, modTitle)
             || !SteamUGC.SetItemDescription(updateHandle, description)
             || !SteamUGC.SetItemContent(updateHandle, buildPath))
            {
                Debug.LogError("StartItemUpdate returned invalid Update Handle");
                return false;
            }

            if (hasThumbnail)
            {
                SteamUGC.SetItemPreview(updateHandle, imagePath);
            }
            SteamAPICall_t updateCallHandle = SteamUGC.SubmitItemUpdate(updateHandle, modification.Manifest.Version);
            CallResult<SubmitItemUpdateResult_t> submitItemUpdateResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdateResult);
            submitItemUpdateResult.Set(updateCallHandle);
            needRunCallbacks = true;
            return SteamRunCallbacks();
        }

        private static void OnSubmitItemUpdateResult(SubmitItemUpdateResult_t callback, bool bIOFailure) 
        {
            if (bIOFailure || callback.m_eResult != EResult.k_EResultOK) 
            {
                failed = true;
                Debug.LogError($"Steam Workshop Update Failed with result: {callback.m_eResult}");
            }
            else 
            {
                failed = false;
                Debug.Log($"Item {callback.m_nPublishedFileId} updated with result {callback.m_eResult}");
            }
            if (callback.m_bUserNeedsToAcceptWorkshopLegalAgreement) Debug.LogWarning("User needs to accept Workshop Legal Agreement");
            needRunCallbacks = false;
        }
        private static bool SteamRunCallbacks(bool isItemCreation = false) {
            int timeoutCounter = 0;
            while (needRunCallbacks && timeoutCounter < 300) 
            {
                SteamAPI.RunCallbacks();
                Thread.Sleep(100);
                timeoutCounter++;
            }

            if (isItemCreation) 
            {
                return PublishWorkshopItem();
            }
            return failed;
        }
    }
}