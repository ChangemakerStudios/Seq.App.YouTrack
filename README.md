Seq.App.YouTrack
=================

[![Build status](https://ci.appveyor.com/api/projects/status/vkogqinnmjjeyh9l/branch/master?svg=true)](https://ci.appveyor.com/project/Jaben/seq-app-youtrack/branch/master)

Seq.App for Posting an Exception to an Issue in YouTrack

Uses Handlebar Syntax for the Template Formatting: e.g. {{$Variable}}

Supports the following fields in the issue created:

* $Id: Logged event id
* $UtcTimestamp: Universal timestamp
* $LocalTimestamp: Local (Seq Instance) timestamp
* $Level: Error Level (e.g. "error", "warning")
* $MessageTemplate: Non-rendered message template.
* $Message: Rendered log message.
* $Exception: Logged exception.
* $Properties: Additional properties for the logged event.
* $EventType: Seq's event type field.
* $Instance: Name of of the Seq instance.
* $ServerUri: Url of the Seq instance.
