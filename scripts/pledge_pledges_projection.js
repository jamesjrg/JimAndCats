Name: pledge-pledges-public
Mode: Continuous
Emits Enabled:true

fromStream('pledge-pledges-private').
    when({
        "PledgeCreated" : function(state,event) {
          emit("pledge-pledges-public", event.eventType, {
            id: event.body.value.Item.Id
          });
    });

