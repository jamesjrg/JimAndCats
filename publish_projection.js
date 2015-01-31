Name: users-public
Mode: Continuous
Emits Enabled:true

fromStream('identity').
    when({
        "UserCreated" : function(state,event) {
          emit("identity-public", event.eventType, {name : event.body.value.Item.Name.Item});
         },
         "NameChanged" : function(state,event) {
          emit("identity-public", event.eventType, {name : event.body.value.Item.Name.Item});
         },
         "EmailChanged" : function(state,event) {
          emit("identity-public", event.eventType, {name : event.body.value.Item.Email.Item});
         }
    });

