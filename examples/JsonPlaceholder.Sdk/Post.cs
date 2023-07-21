using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonPlaceholder.Sdk
{
    public class Post
    {
        public int UserId { get; set; } = default;
        public int Id { get; set; } = default;
        public string Title { get; set; } = default!;
        public string Body { get; set; } = default!;
    }
}
