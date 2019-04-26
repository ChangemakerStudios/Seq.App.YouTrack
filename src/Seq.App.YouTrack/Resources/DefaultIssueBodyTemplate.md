Log Level: **{{$Level}}**
Event ID: **#{{$Id}}**
Project ID: **{{$YouTrackProjectId}}**
Timestamp: {{$LocalTimestamp}}
Link to Seq: [Direct Link]({{$ServerUri}}#/events?filter=@Id%3D%3D%22{{$Id}}%22)

----

### Message
```
{{$Message}}
```

{{#if $Exception}}
#### Exception
~~~stacktrace
{{$Exception}}
~~~
{{/if}}

#### Properties
```jscript
{{#each $Properties}}
{ {{@key}} : {{pretty this}} }
{{/each}}
```