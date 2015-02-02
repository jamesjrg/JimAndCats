#load "EventStoreProjections.fsx"

open EventStoreProjections

let projection = """fromStream("jim-users-private").
    when({
        "UserCreated" : function(state,event) {
          emit("jim-users-public", event.eventType, {
            id: event.body.value.Item.Id,
            name : event.body.value.Item.Name.Item,
            email : event.body.value.Item.Email.Item,
            creationTime : event.body.value.Item.CreationTime.Ticks
          });
         },
         "NameChanged" : function(state,event) {
          emit("jim-users-public", event.eventType, {
            id: event.body.value.Item.Id,
            name : event.body.value.Item.Name.Item});
         },
         "EmailChanged" : function(state,event) {
          emit("jim-users-public", event.eventType, {
            id: event.body.value.Item.Id,
            name : event.body.value.Item.Email.Item});
         }
    })
"""

postProjection "jim-users-public" projection