using Files.Shared;
using Files.Shared.Extensions;
using Files.App.Helpers;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using IO = System.IO;
using Files.App.Shell;

namespace Files.App.Filesystem.StorageItems
{
    public class ShortcutStorageFile : ShellStorageFile, IShortcutStorageItem
    {
        public string TargetPath { get; }
        public string Arguments { get; }
        public string WorkingDirectory { get; }
        public bool RunAsAdmin { get; }

        public ShortcutStorageFile(ShellLinkItem item) : base(item)
        {
            TargetPath = item.TargetPath;
            Arguments = item.Arguments;
            WorkingDirectory = item.WorkingDirectory;
            RunAsAdmin = item.RunAsAdmin;
        }
    }

    public class BinStorageFile : ShellStorageFile, IBinStorageItem
    {
        public string OriginalPath { get; }
        public DateTimeOffset DateDeleted { get; }

        public BinStorageFile(ShellFileItem item) : base(item)
        {
            OriginalPath = item.FilePath;
            DateDeleted = item.RecycleDate;
        }
    }

    public class ShellStorageFile : BaseStorageFile
    {
        public override string Path { get; }
        public override string Name { get; }
        public override string DisplayName => Name;
        public override string ContentType => "application/octet-stream";
        public override string FileType => IO.Path.GetExtension(Name);
        public override string FolderRelativeId => $"0\\{Name}";

        public override string DisplayType { get; }

        public override DateTimeOffset DateCreated { get; }

        public override Windows.Storage.FileAttributes Attributes => Windows.Storage.FileAttributes.Normal | Windows.Storage.FileAttributes.ReadOnly;

        private IStorageItemExtraProperties properties;
        public override IStorageItemExtraProperties Properties => properties ??= new BaseBasicStorageItemExtraProperties(this);

        public ShellStorageFile(ShellFileItem item)
        {
            Name = item.FileName;
            Path = item.RecyclePath; // True path on disk
            DateCreated = item.CreatedDate;
            DisplayType = item.FileType;
        }

        public override IAsyncOperation<StorageFile> ToStorageFileAsync() => throw new NotSupportedException();

        public static ShellStorageFile FromShellItem(ShellFileItem item)
        {
            if (item is ShellLinkItem linkItem)
            {
                return new ShortcutStorageFile(linkItem);
            }
            else if (item.RecyclePath.Contains("$Recycle.Bin", StringComparison.Ordinal))
            {
                return new BinStorageFile(item);
            }
            else
            {
                return new ShellStorageFile(item);
            }
        }

        public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
        {
            if (ShellStorageFolder.IsShellPath(path))
            {
                if (GetFile(path) is ShellFileItem file)
                {
                    return Task.FromResult<BaseStorageFile>(FromShellItem(file)).AsAsyncOperation();
                }
            }
            return Task.FromResult<BaseStorageFile>(null).AsAsyncOperation();
        }

        private static ShellFileItem GetFile(string path)
        {
            using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPidl(path);
            return ShellFolderExtensions.GetShellFileItem(shellItem);
        }

        public override bool IsEqual(IStorageItem item) => item?.Path == Path;
        public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();
        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync() => GetBasicProperties().AsAsyncOperation();

        public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName) => throw new NotSupportedException();
        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) => throw new NotSupportedException();

        public override IAsyncAction DeleteAsync() => throw new NotSupportedException();
        public override IAsyncAction DeleteAsync(StorageDeleteOption option) => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (ShellStorageFolder.IsShellPath(Path))
                {
                    return null;
                }
                var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                return await zipFile.GetThumbnailAsync(mode);
            });
        }
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (ShellStorageFolder.IsShellPath(Path))
                {
                    return null;
                }
                var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                return await zipFile.GetThumbnailAsync(mode, requestedSize);
            });
        }
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (ShellStorageFolder.IsShellPath(Path))
                {
                    return null;
                }
                var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                return await zipFile.GetThumbnailAsync(mode, requestedSize, options);
            });
        }

        public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName) => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) => throw new NotSupportedException();

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode) => throw new NotSupportedException();
        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => throw new NotSupportedException();
        public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync() => throw new NotSupportedException();
        public override IAsyncOperation<IInputStream> OpenSequentialReadAsync() => throw new NotSupportedException();

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync() => throw new NotSupportedException();
        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options) => throw new NotSupportedException();

        public override IAsyncAction RenameAsync(string desiredName) => throw new NotSupportedException();
        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => throw new NotSupportedException();

        private Task<BaseBasicProperties> GetBasicProperties()
        {
            if (GetFile(Path) is ShellFileItem file)
            {
                return Task.FromResult<BaseBasicProperties>(new ShellFileBasicProperties(file));
            }
            return Task.FromResult(new BaseBasicProperties());
        }

        private class ShellFileBasicProperties : BaseBasicProperties
        {
            private readonly ShellFileItem file;

            public ShellFileBasicProperties(ShellFileItem folder) => this.file = folder;

            public override ulong Size => file.FileSizeBytes;

            public override DateTimeOffset ItemDate => file.ModifiedDate;
            public override DateTimeOffset DateModified => file.ModifiedDate;
        }
    }
}
