### Introduction

This is the rationale of what we are building.

We will build a component that facilitates the following:

1. **Uploading** (storage) of a file
2. **Retrieval** (download) of a file
3. **Deletion** of a file

Upon upload of a file, we need to generate some sort of **identifier** that can be used in the application.

Also, we probably will need to store some **metadata** to make it easier to implement some functionality in the application - for example a page to view file details, icon, etc.

This metadata will include:

1. File **name**
2. File **size** (in bytes)
3. **Extension** (Will need this to know how to render the file if being viewed by the browser)
4. **Date Uploaded**
5. File **Hash** (Hash to detect changes to the file (for whatever reason). Also to tell if this file has been uploaded before)

We can then improve this component by performing some operations before persistence. At present these will include:

1. **Compression** - whenever possible, cut down on storage
2. **Encryption** - in this age of hackers and mistakes, better encrypt the file contents in case the storage is ever breached.

With regard to storage, this component should support the following:

1. **File system** - the files will be stored on a folder in the server
2. **Database** - the files will be stored as [BLOBs](https://en.wikipedia.org/wiki/Binary_blob) on the database. Preliminary support will be for [SQL Server](https://www.microsoft.com/en-us/sql-server) first, and then [PostgreSQL](https://www.postgresql.org/)
3. **Cloud BLOB storage** - the files will be stored as BLOB objects in the cloud. Preliminary support will be for [Azure](https://azure.microsoft.com/en-us/) and [Amazon](https://aws.amazon.com/).

The component itself should support dependency injection, and should be configurable at this point in terms of:

- Storage, & settings
- Compression & settings
- Encryption & settings

The dependency injection requirement will make it easy to use for

- APIs
- Web applications
- Console applications
- Service applications

We will build it in such a way to make it extensible so that it will be easy to support:

- Other databases - MySQL, SQLite
- Other BLOB storage providers - [Google](https://cloud.google.com/), [Dreamhost](https://www.dreamhost.com/cloud/storage/), [Hetzner](https://www.hetzner.com/storage/object-storage/), [Heroku](https://elements.heroku.com/addons/ah-s3-object-storage-stackhero)

Finally, some (preliminary) deliberate decisions

### Uniqueness

If you upload two files with the same name, **the system will treat them as different** and store both and give you two different IDs. We will not make any effort to detect and prevent duplicates (either by file name, or by contents)

### Context

Files are usually uploaded with some **context** - e.g. an upload file will belong to the logged-in user. **This component will make no effort to preserve this - that will be responsibility of the application**. The component will purely deal with the file alone.

### Changes Of Settings

Given we are going to support encryption and compression, it will probably be a good idea to persist whatever encryption algorithm and compression algorithm were used at the point of storage as part of the metadata. This way should we need to change them, updating existing files will be much easier. It will look repetitive, but this is an acceptable choice to balance future changes.

### Hashing

We will use SHA256

### Encryption

We will use AES

### Compression

We will use Zip compression

### File IDs

We will use Guid as file IDs

### Update

There will be **no support for update**. To update, **delete** the existing and **upload** the replacement.