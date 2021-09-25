﻿using Dead_By_Daylight_Mod_Installer.Consts;
using Dead_By_Daylight_Mod_Installer.Model;
using Dead_By_Daylight_Mod_Installer.Services;
using Dead_By_Daylight_Mod_Installer.Services.Interfaces;
using Dead_By_Daylight_Mod_Installer.View;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dead_By_Daylight_Mod_Installer.Presenter
{
    public class InstallerPresenter
    {
        private readonly IInstallerView view;
        private readonly IMessageBoxService messageBoxService;
        private readonly IPickerService pickerService;
        private readonly IPatcherService patcherService;
        private readonly IPackageService packageService;

        public InstallerPresenter(IInstallerView view, IPackageService packageService, IMessageBoxService messageBoxService, IPickerService pickerService, IPatcherService patcherService)
        {
            this.view = view;
            this.view.Presenter = this;
            this.messageBoxService = messageBoxService;
            this.pickerService = pickerService;
            this.patcherService = patcherService;
            this.packageService = packageService;

            view.PaksPath = Properties.Settings.Default.PaksPath;
        }

        public void DisplayCreator()
        {
            var creatorView = new CreatorForm();

            var creatorPresenter = new CreatorPresenter(creatorView, new PackageService(), new MessageBoxService(), new PickerService());
            creatorView.ShowDialog();
        }

        public void InstallMod()
        {
            if (!Directory.Exists(Properties.Settings.Default.PaksPath))
            {
                return;
            }

            var pickResult = pickerService.PickFilePath(out string modFilePath, Constants.ModPackageFilter);
            if (pickResult == Enums.PickResult.Ok)
            {
                var modPackageFormat = packageService.GetFormat(modFilePath);
                if (modPackageFormat != Enums.ModPackageFormat.Unknown)
                {
                    ModPackage modPackage = packageService.ReadPackage(modFilePath, modPackageFormat);
                    foreach (var mod in modPackage.Mods)
                    {
                        if (messageBoxService.Question($"Do you want to install \"{mod.Title}\"?"))
                        {
                            string pakFilePath = Path.Combine(Properties.Settings.Default.PaksPath, mod.PakName);
                            if (!File.Exists(pakFilePath))
                            {
                                messageBoxService.ShowMessage($"Mod Installer Can't find \"{mod.PakName}\" file, make sure that specified pak folder path still valid.");
                            }
                            //TODO: stop blocking the thread
                            else if (Task.Run(async () => await patcherService.FindAndReplaceBytes(pakFilePath, mod.OriginalBytes, mod.ModifiedBytes)).Result)
                            {
                                messageBoxService.ShowMessage($"\"{mod.Title}\" Mod has been successfully installed!");
                            }
                            else
                            {
                                messageBoxService.ShowMessage($"An error occured when tried to install \"{mod.Title}\" mod, make sure that mod isn't already installed, game isn't running and mod package was properly made.");
                            }
                        }
                    }
                }
                else
                {
                    messageBoxService.ShowMessage("Unrecognized mod package format");
                }
            }
            else if (pickResult == Enums.PickResult.None)
            {
                messageBoxService.ShowMessage("Mod Installer Can't get access to mod package file.");
            }
        }

        public void UninstallMod()
        {
            if (!Directory.Exists(Properties.Settings.Default.PaksPath))
            {
                return;
            }

            var pickResult = pickerService.PickFilePath(out string modFilePath, Constants.ModPackageFilter);
            if (pickResult == Enums.PickResult.Ok)
            {
                var modPackageFormat = packageService.GetFormat(modFilePath);
                if (modPackageFormat != Enums.ModPackageFormat.Unknown)
                {
                    ModPackage modPackage = packageService.ReadPackage(modFilePath, modPackageFormat);
                    foreach (var mod in modPackage.Mods)
                    {
                        if (messageBoxService.Question($"Do you want to uninstall \"{mod.Title}\"?"))
                        {
                            string pakFilePath = Path.Combine(Properties.Settings.Default.PaksPath, mod.PakName);
                            if (!File.Exists(pakFilePath))
                            {
                                messageBoxService.ShowMessage($"Mod Installer Can't find \"{mod.PakName}\" file, make sure that specified pak folder path still valid.");
                            }
                            //TODO: stop blocking the thread
                            else if (Task.Run(async () => await patcherService.FindAndReplaceBytes(pakFilePath, mod.ModifiedBytes, mod.OriginalBytes)).Result)
                            {
                                messageBoxService.ShowMessage($"\"{mod.Title}\" Mod successfully uninstalled!");
                            }
                            else
                            {
                                messageBoxService.ShowMessage($"An error occured when tried to uninstall \"{mod.Title}\" mod, make sure that mod even installed, game isn't running and mod package was properly made.");
                            }
                        }
                    }
                }
                else
                {
                    messageBoxService.ShowMessage("Unrecognized mod package format");
                }
            }
            else if (pickResult == Enums.PickResult.None)
            {
                messageBoxService.ShowMessage("Mod Installer Can't get access to mod package file.");
            }
        }

        public void ChangePaksPath(string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath))
            {
                return;
            }

            Properties.Settings.Default.PaksPath = newPath;
            Properties.Settings.Default.Save();

            view.PaksPath = newPath;
        }
    }
}
