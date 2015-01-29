Name: users-public
Mode: Continuous
Emits Enabled:true

fromStream('users').
    when({
        "UserCreated" : function(state,event) {
          emit("users-public", event.type, {"name" : event.body.value.Item.Name.Item})
         },
         "NameChanged" : function(state,event) {
          emit("users-public", event.type, {"name" : event.body.value.Item.Name.Item})
         },
         "EmailChanged" : function(state,event) {
          emit("users-public", event.type, {"name" : event.body.value.Item.Email.Item})
         }
    });
