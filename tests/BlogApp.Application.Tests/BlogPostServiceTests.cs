using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Concrete;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Application.Tests;

public sealed class BlogPostServiceTests : IDisposable
{
    private readonly TestDatabase db = new();
    private readonly BlogPostService service;
    private readonly User author;
    private readonly User otherAuthor;

    public BlogPostServiceTests()
    {
        service = new BlogPostService(db.Context, db.Clock);
        author = db.AddUser("yazar@example.com");
        otherAuthor = db.AddUser("diger@example.com");
    }

    public void Dispose() => db.Dispose();

    private SaveBlogPostRequest Post(string title = "Çalışan Yazılım Ekipleri", bool publish = true, int? authorId = null, int? categoryId = null) => new()
    {
        AuthorId = authorId ?? author.Id,
        CategoryId = categoryId ?? db.SoftwareCategoryId,
        Title = title,
        Content = "## Giriş\n\nİyi ekipler küçük adımlarla ilerler.",
        Publish = publish,
    };

    [Fact]
    public async Task Publishing_creates_a_turkish_friendly_slug_and_a_publish_date()
    {
        var result = await service.CreateAsync(Post());

        Assert.True(result.Success);
        Assert.Equal("calisan-yazilim-ekipleri", result.Data);

        var saved = await db.CreateContext().BlogPosts.SingleAsync();
        Assert.Equal(BlogPostStatus.Approved, saved.Status);
        Assert.Equal(db.Clock.GetUtcNow().UtcDateTime, saved.PublishedAt);
        Assert.Contains("İyi ekipler küçük adımlarla ilerler", saved.Excerpt);
    }

    [Fact]
    public async Task Two_posts_with_the_same_title_get_different_slugs()
    {
        var first = await service.CreateAsync(Post());
        var second = await service.CreateAsync(Post(authorId: otherAuthor.Id));

        Assert.Equal("calisan-yazilim-ekipleri", first.Data);
        Assert.Equal("calisan-yazilim-ekipleri-2", second.Data);
    }

    [Fact]
    public async Task A_draft_is_visible_only_to_its_author()
    {
        var slug = (await service.CreateAsync(Post(publish: false))).Data!;

        var asAuthor = await service.GetBySlugAsync(slug, author.Id);
        var asSomeoneElse = await service.GetBySlugAsync(slug, otherAuthor.Id);
        var anonymous = await service.GetBySlugAsync(slug, null);
        var list = await service.GetPublishedAsync(new PostListQuery());

        Assert.True(asAuthor.Success);
        Assert.True(asAuthor.Data!.IsOwnedByViewer);
        Assert.False(asSomeoneElse.Success);
        Assert.False(anonymous.Success);
        Assert.Equal(0, list.TotalCount);
    }

    [Fact]
    public async Task Only_the_author_can_edit_or_delete_a_post()
    {
        await service.CreateAsync(Post());
        var postId = (await db.CreateContext().BlogPosts.SingleAsync()).Id;

        var edit = await service.UpdateAsync(postId, Post(title: "Başka bir başlık", authorId: otherAuthor.Id));
        var delete = await service.DeleteAsync(postId, otherAuthor.Id);

        Assert.False(edit.Success);
        Assert.False(delete.Success);
        Assert.Equal("Çalışan Yazılım Ekipleri", (await db.CreateContext().BlogPosts.SingleAsync()).Title);
    }

    [Fact]
    public async Task A_published_post_keeps_its_url_when_the_title_changes()
    {
        var slug = (await service.CreateAsync(Post())).Data!;
        var postId = (await db.CreateContext().BlogPosts.SingleAsync()).Id;

        var result = await service.UpdateAsync(postId, Post(title: "Yeni Başlık"));

        Assert.True(result.Success);
        Assert.Equal(slug, result.Data);
    }

    [Fact]
    public async Task A_draft_gets_a_new_url_when_the_title_changes()
    {
        await service.CreateAsync(Post(publish: false));
        var postId = (await db.CreateContext().BlogPosts.SingleAsync()).Id;

        var result = await service.UpdateAsync(postId, Post(title: "Yeni Başlık", publish: false));

        Assert.Equal("yeni-baslik", result.Data);
    }

    [Fact]
    public async Task Saving_a_published_post_as_a_draft_takes_it_offline()
    {
        await service.CreateAsync(Post());
        var postId = (await db.CreateContext().BlogPosts.SingleAsync()).Id;

        var result = await service.UpdateAsync(postId, Post(publish: false));

        Assert.True(result.Success);
        Assert.Equal(BlogPostStatus.Draft, (await db.CreateContext().BlogPosts.SingleAsync()).Status);
    }

    [Fact]
    public async Task An_author_cannot_republish_a_post_an_admin_took_down()
    {
        await service.CreateAsync(Post());
        await db.Context.BlogPosts.ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, BlogPostStatus.Rejected));
        db.Context.ChangeTracker.Clear();
        var postId = (await db.CreateContext().BlogPosts.SingleAsync()).Id;

        var result = await service.UpdateAsync(postId, Post());

        Assert.False(result.Success);
        Assert.Equal(BlogPostStatus.Rejected, (await db.CreateContext().BlogPosts.SingleAsync()).Status);
    }

    [Fact]
    public async Task A_disabled_account_cannot_write()
    {
        var disabled = db.AddUser("kapali@example.com", active: false);

        var result = await service.CreateAsync(Post(authorId: disabled.Id));

        Assert.False(result.Success);
        Assert.Empty(db.CreateContext().BlogPosts);
    }

    [Fact]
    public async Task An_unknown_category_is_rejected()
    {
        var result = await service.CreateAsync(Post(categoryId: 9999));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task The_public_list_filters_by_category_and_search_and_pages_newest_first()
    {
        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(Post(title: $"Yazılım notu {i}"));
            db.Clock.Advance(TimeSpan.FromHours(1));
        }

        await service.CreateAsync(Post(title: "Mülakata hazırlık", categoryId: db.CareerCategoryId));
        await service.CreateAsync(Post(title: "Taslak not", publish: false));

        var firstPage = await service.GetPublishedAsync(new PostListQuery { CategoryId = db.SoftwareCategoryId, PageSize = 2 });
        var lastPage = await service.GetPublishedAsync(new PostListQuery { CategoryId = db.SoftwareCategoryId, PageSize = 2, Page = 99 });
        var search = await service.GetPublishedAsync(new PostListQuery { Search = "Mülakat" });

        Assert.Equal(5, firstPage.TotalCount);
        Assert.Equal(new[] { "Yazılım notu 5", "Yazılım notu 4" }, firstPage.Items.Select(p => p.Title));
        Assert.Equal(3, lastPage.Page);
        Assert.Equal("Yazılım notu 1", Assert.Single(lastPage.Items).Title);
        Assert.Equal("Mülakata hazırlık", Assert.Single(search.Items).Title);
    }
}
