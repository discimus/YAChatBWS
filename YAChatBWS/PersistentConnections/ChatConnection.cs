using Bogus;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YAChatBWS.PersistentConnections
{
    public class ChatConnection : PersistentConnection
    {
        private static List<User> _users = new List<User>();

        public class User
        {
            public string UserId { get; set; }
            public string UserAvatarUrl { get; set; }
            public string UserNickName { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public class ChatMessage
        {
            public ChatMessageType ChatMessageType { get; set; }
            public string Payload { get; set; }
            public User Sender { get; set; }
            public DateTime Instant { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public enum ChatMessageType
        {
            BroadcastMessage = 1,
            ConnectedSuccessfullyMessage = 2,
            ServerInfo = 3,
        }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            var faker = new Faker();

            var user = new User()
            {
                UserId = connectionId,
                UserNickName = faker.Internet.UserName(),
                UserAvatarUrl = faker.Internet.Avatar()
            };

            _users.Add(user);

            ChatMessage infoUserNickName = new ChatMessage()
            {
                ChatMessageType = ChatMessageType.ConnectedSuccessfullyMessage,
                Payload = user.ToString()
            };

            Connection.Send(connectionId, infoUserNickName.ToString());

            ChatMessage serverInfoNewUserMessage = new ChatMessage()
            {
                ChatMessageType = ChatMessageType.ServerInfo,
                Payload = $"User '{user.UserNickName}' joinned the chat.",
                Instant = DateTime.Now,
                Sender = new User()
                {
                    UserNickName = "Server",
                    UserAvatarUrl = "https://dummyimage.com/64"
                }
            };

            Connection.Broadcast(serverInfoNewUserMessage.ToString());

            return Task.CompletedTask;
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            try
            {
                ChatMessage chatMessage = JsonConvert.DeserializeObject<ChatMessage>(data);

                User user = _users.First(t => t.UserId == connectionId);

                ChatMessage serverMessage = new ChatMessage()
                {
                    ChatMessageType = ChatMessageType.BroadcastMessage,
                    Payload = chatMessage.Payload,
                    Instant = DateTime.Now,
                    Sender = new User()
                    {
                        UserId = user.UserId,
                        UserNickName = user.UserNickName,
                        UserAvatarUrl = user.UserAvatarUrl
                    }
                };

                return Connection.Broadcast(serverMessage.ToString());
            }
            catch (Exception ex)
            {
                ChatMessage serverMessage = new ChatMessage()
                {
                    //Payload = "You are traying to send messages, but they're in an invalid format."
                    Payload = ex.ToString()
                };

                return Connection.Send(connectionId, serverMessage.ToString());
            }
        }

        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            List<User> usersWithId = _users
                .Where(t => t.UserId == connectionId)
                .ToList();

            if (usersWithId.Any())
            {
                User user = usersWithId.First();

                ChatMessage message = new ChatMessage()
                {
                    ChatMessageType = ChatMessageType.ServerInfo,
                    Payload = $"User '{user.UserNickName}' left the chat.",
                    Instant = DateTime.Now,
                    Sender = new User()
                    {
                        UserNickName = "Server",
                        UserAvatarUrl = "https://dummyimage.com/64"
                    }
                };

                Connection.Broadcast(message.ToString());
            }

            return Task.CompletedTask;
        }

        //protected override Task OnDisconnected(string clientId)
        //{
        //    return Connection.Broadcast("Client " + clientId + " disconncted");
        //}
    }
}