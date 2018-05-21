﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace VisualObjects.ActorService
{
  using System;
  using System.Threading.Tasks;
  using VisualObjects.Common;
  using Microsoft.ServiceFabric.Actors.Runtime;
  using Microsoft.ServiceFabric.Actors;

  [ActorService(Name = "VisualObjects.ActorService")]
  [StatePersistence(StatePersistence.Persisted)]
  public class VisualObjectActor : Actor, IVisualObjectActor
  {

    private static readonly string StatePropertyName = "VisualObject";
    private IActorTimer updateTimer;
    private string jsonString;

    public VisualObjectActor(ActorService actorService, ActorId actorId)
      : base(actorService, actorId)
    {
      ActorEventSource.Current.Message("VisualObjectActor: CREATE");
    }

    ~VisualObjectActor()
    {
      ActorEventSource.Current.Message("VisualObjectActor: DESTROY");
    }

    public Task<string> GetStateAsJsonAsync()
    {
      return Task.FromResult(this.jsonString);
    }

    protected override async Task OnActivateAsync()
    {
      ActorEventSource.Current.Message("VisualObjectActor: ACTIVATE");
      VisualObject newObject = VisualObject.CreateRandom(this.Id.ToString());

      ActorEventSource.Current.ActorMessage(this, "StateCheck {0}",
        (await this.StateManager.ContainsStateAsync(StatePropertyName)).ToString());

      VisualObject result = await this.StateManager.GetOrAddStateAsync<VisualObject>(StatePropertyName, newObject);

      this.jsonString = result.ToJson();

      // ACTOR MOVEMENT REFRESH
      this.updateTimer = this.RegisterTimer(this.MoveObject, null, TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(100));
      return;
    }

    private async Task MoveObject(object obj)
    {
      VisualObject visualObject = await this.StateManager.GetStateAsync<VisualObject>(StatePropertyName);

      //alternate which lines are commented out
      //then do an upgrade to cause the
      //visual objects to start rotating

      visualObject.Move(false);
      //visualObject.Move(true);

      await this.StateManager.SetStateAsync<VisualObject>(StatePropertyName, visualObject);

      this.jsonString = visualObject.ToJson();

      return;
    }

    protected override Task OnDeactivateAsync()
    {
      ActorEventSource.Current.Message("VisualObjectActor: DEACTIVATE");
      return base.OnDeactivateAsync();
    }
  }
}