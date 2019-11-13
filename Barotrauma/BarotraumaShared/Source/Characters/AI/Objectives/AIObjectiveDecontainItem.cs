﻿using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace Barotrauma
{
    class AIObjectiveDecontainItem : AIObjective
    {
        public override string DebugTag => "decontain item";

        public Func<Item, float> GetItemPriority;

        //can either be a tag or an identifier
        private readonly string[] itemIdentifiers;
        private readonly ItemContainer sourceContainer;
        private ItemContainer targetContainer;
        private readonly Item targetItem;

        private AIObjectiveGoTo goToObjective;
        private AIObjectiveContainItem containObjective;

        public bool Equip { get; set; }

        public AIObjectiveDecontainItem(Character character, Item targetItem, AIObjectiveManager objectiveManager, ItemContainer sourceContainer = null, ItemContainer targetContainer = null, float priorityModifier = 1) 
            : base(character, objectiveManager, priorityModifier)
        {
            this.targetItem = targetItem;
            this.sourceContainer = sourceContainer;
            this.targetContainer = targetContainer;
        }

        public AIObjectiveDecontainItem(Character character, string itemIdentifier, AIObjectiveManager objectiveManager, ItemContainer sourceContainer, ItemContainer targetContainer = null, float priorityModifier = 1) 
            : this(character, new string[] { itemIdentifier }, objectiveManager, sourceContainer, targetContainer, priorityModifier) { }

        public AIObjectiveDecontainItem(Character character, string[] itemIdentifiers, AIObjectiveManager objectiveManager, ItemContainer sourceContainer, ItemContainer targetContainer = null, float priorityModifier = 1) 
            : base(character, objectiveManager, priorityModifier)
        {
            this.itemIdentifiers = itemIdentifiers;
            for (int i = 0; i < itemIdentifiers.Length; i++)
            {
                itemIdentifiers[i] = itemIdentifiers[i].ToLowerInvariant();
            }
            this.sourceContainer = sourceContainer;
            this.targetContainer = targetContainer;
        }

        protected override bool Check() => IsCompleted;

        public override float GetPriority()
        {
            if (objectiveManager.CurrentOrder == this)
            {
                return AIObjectiveManager.OrderPriority;
            }
            return 1.0f;
        }

        protected override void Act(float deltaTime)
        {
            Item itemToDecontain = targetItem ?? sourceContainer.Inventory.FindItem(i => itemIdentifiers.Any(id => i.Prefab.Identifier == id || i.HasTag(id)), recursive: false);
            if (itemToDecontain == null)
            {
                Abandon = true;
                return;
            }
            if (targetContainer == null)
            {
                if (sourceContainer == null)
                {
                    Abandon = true;
                    return;
                }
                if (itemToDecontain.Container != sourceContainer.Item)
                {
                    IsCompleted = true;
                    return;
                }
            }
            else
            {
                if (targetContainer.Inventory.Items.Contains(itemToDecontain))
                {
                    IsCompleted = true;
                    return;
                }
            }
            if (goToObjective == null && !itemToDecontain.IsOwnedBy(character))
            {
                if (sourceContainer == null)
                {
                    Abandon = true;
                    return;
                }
                if (!character.CanInteractWith(sourceContainer.Item, out _, checkLinked: false))
                {
                    TryAddSubObjective(ref goToObjective,
                        constructor: () => new AIObjectiveGoTo(sourceContainer.Item, character, objectiveManager),
                        onAbandon: () => Abandon = true);
                    return;
                }
            }
            if (targetContainer != null)
            {
                TryAddSubObjective(ref containObjective,
                    constructor: () => new AIObjectiveContainItem(character, itemToDecontain, targetContainer, objectiveManager)
                    {
                        Equip = this.Equip,
                        GetItemPriority = this.GetItemPriority,
                        ignoredContainerIdentifiers = sourceContainer != null ? new string[] { sourceContainer.Item.Prefab.Identifier } : null
                    },
                    onCompleted: () => IsCompleted = true,
                    onAbandon: () => targetContainer = null);
            }
            else
            {
                itemToDecontain.Drop(character);
                IsCompleted = true;
            }
        }
    }
}
