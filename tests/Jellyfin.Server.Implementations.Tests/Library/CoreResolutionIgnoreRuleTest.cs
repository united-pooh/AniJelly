using System;
using System.IO;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class CoreResolutionIgnoreRuleTest
{
    private readonly CoreResolutionIgnoreRule _rule;
    private readonly NamingOptions _namingOptions;
    private readonly Mock<IServerApplicationPaths> _appPathsMock;

    public CoreResolutionIgnoreRuleTest()
    {
        _namingOptions = new NamingOptions();

        _namingOptions.AllExtrasTypesFolderNames.TryAdd("extras", ExtraType.Trailer);

        _appPathsMock = new Mock<IServerApplicationPaths>();
        _appPathsMock.SetupGet(x => x.RootFolderPath).Returns("/server/root");

        _rule = new CoreResolutionIgnoreRule(_namingOptions, _appPathsMock.Object);
    }

    private FileSystemMetadata MakeFileSystemMetadata(string fullName, bool isDirectory = false)
        => new FileSystemMetadata { FullName = fullName, Name = Path.GetFileName(fullName), IsDirectory = isDirectory };

    private BaseItem MakeParent(string name = "Parent", bool isTopParent = false, Type? type = null)
    {
        return type switch
        {
            Type t when t == typeof(Folder) => CreateMock<Folder>(name, isTopParent).Object,
            Type t when t == typeof(AggregateFolder) => CreateMock<AggregateFolder>(name, isTopParent).Object,
            Type t when t == typeof(UserRootFolder) => CreateMock<UserRootFolder>(name, isTopParent).Object,
            _ => CreateMock<BaseItem>(name, isTopParent).Object
        };
    }

    private static Mock<T> CreateMock<T>(string name, bool isTopParent)
    where T : BaseItem
    {
        var mock = new Mock<T>();
        mock.SetupGet(p => p.Name).Returns(name);
        mock.SetupGet(p => p.IsTopParent).Returns(isTopParent);
        return mock;
    }

    [Fact]
    public void TestApplicationFolder()
    {
        Assert.False(_rule.ShouldIgnore(
            MakeFileSystemMetadata("/server/root/extras", isDirectory: true),
            null));

        Assert.False(_rule.ShouldIgnore(
            MakeFileSystemMetadata("/server/root/small.jpg"),
            null));
    }

    [Theory]
    [InlineData("CDs")]
    [InlineData("cds")]
    [InlineData("NCOP")]
    [InlineData("NCED")]
    public void ShouldIgnore_AnimeResourceDirectory_DoesNotCreateSeason(string folder)
    {
        var path = "/Anime/Show/" + folder;

        Assert.True(_rule.ShouldIgnore(MakeFileSystemMetadata(path, true), new Series()));
        Assert.True(_rule.ShouldIgnore(MakeFileSystemMetadata(path, true), new Season()));
    }

    [Fact]
    public void ShouldIgnore_CdDirectoryInMusicLibrary_IsPreserved()
    {
        Assert.False(_rule.ShouldIgnore(MakeFileSystemMetadata("/Music/Album/CDs", true), new MusicAlbum()));
        Assert.False(_rule.ShouldIgnore(MakeFileSystemMetadata("/Media/CDs", true), new AggregateFolder()));
        Assert.False(_rule.ShouldIgnore(MakeFileSystemMetadata("/Anime/Show/CDs.mkv"), new Series()));
        Assert.False(_rule.ShouldIgnore(MakeFileSystemMetadata("/Anime/Show/CDs Collection", true), new Series()));
    }

    [Fact]
    public void TestTopLevelDirectory()
    {
        Assert.False(_rule.ShouldIgnore(
            MakeFileSystemMetadata("Series/Extras", true),
            MakeParent(type: typeof(AggregateFolder))));

        Assert.False(_rule.ShouldIgnore(
            MakeFileSystemMetadata("Series/Extras/Extras", true),
            MakeParent(isTopParent: true)));
    }

    [Fact]
    public void TestIgnorePatterns()
    {
        Assert.False(_rule.ShouldIgnore(
            MakeFileSystemMetadata("/Media/big.jpg"),
            MakeParent()));

        Assert.True(_rule.ShouldIgnore(
            MakeFileSystemMetadata("/Media/small.jpg"),
            MakeParent()));
    }

    [Fact]
    public void TestExtrasTypesFolderNames()
    {
        FileSystemMetadata fileSystemMetadata = MakeFileSystemMetadata("/Movies/Up/extras", true);

        Assert.False(_rule.ShouldIgnore(
            fileSystemMetadata,
            MakeParent(type: typeof(AggregateFolder))));

        Assert.False(_rule.ShouldIgnore(
            fileSystemMetadata,
            MakeParent(type: typeof(UserRootFolder))));

        Assert.False(_rule.ShouldIgnore(
            fileSystemMetadata,
            null));

        Assert.True(_rule.ShouldIgnore(
            fileSystemMetadata,
            MakeParent()));

        Assert.True(_rule.ShouldIgnore(
            fileSystemMetadata,
            MakeParent(type: typeof(Folder))));
    }

    [Fact]
    public void TestThemeSong()
    {
        Assert.False(_rule.ShouldIgnore(
            MakeFileSystemMetadata("/Movies/Up/intro.mp3"),
            MakeParent()));

        Assert.True(_rule.ShouldIgnore(
            MakeFileSystemMetadata("/Movies/Up/theme.mp3"),
            MakeParent()));
    }
}
