#load "EventStoreProjections.fsx"

open EventStoreProjections

let projection = """
fromStream("pledge-pledges-private").
    when({
        "PledgeCreated" : function(state,event) {
          emit("pledge-pledges-public", event.eventType, {
            id: event.body.value.Item.Id
          });
        }
    });"""

postProjection "pledge-pledges-public" projection